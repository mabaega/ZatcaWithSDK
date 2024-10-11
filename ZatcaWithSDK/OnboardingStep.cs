using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using Zatca.EInvoice.SDK;
using Zatca.EInvoice.SDK.Contracts.Models;
using static ZatcaWithSDK.Models;

namespace ZatcaWithSDK
{
    public class OnboardingStep
    {
        private static readonly HttpClient _httpClient = new();
        public static async Task<CertificateInfo> DeviceOnboarding()
        {
            CertificateInfo certInfo = new();

            try
            {
                if (!Step1_GenerateCSR(certInfo))
                {
                    throw new Exception("Step 1 failed: CSR generation failed.");
                }

                if (!await Step2_GetCCSID(certInfo))
                {
                    throw new Exception("Step 2 failed: Getting CCSID failed.");
                }

                if (!await Step3_SendSampleDocuments(certInfo))
                {
                    throw new Exception("Step 3 failed: Sending sample documents failed.");
                }

                if (!await Step4_GetPCSID(certInfo))
                {
                    throw new Exception("Step 4 failed: Getting PCSID failed.");
                }

                return certInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during onboarding: {ex.Message}");
                throw;
            }
        }
        private static bool Step1_GenerateCSR(CertificateInfo certInfo)
        {
            Console.WriteLine("\nStep 1: Generating CSR and PrivateKey");

            CsrGenerator csrGenerator = new();
            CsrResult csrResult = csrGenerator.GenerateCsr(AppConfig.CsrGenerationDto, AppConfig.EnvironmentType, false);

            if (!csrResult.IsValid)
            {
                Console.WriteLine("Failed to generate CSR");
                return false;
            }

            certInfo.GeneratedCsr = csrResult.Csr;
            certInfo.PrivateKey = csrResult.PrivateKey;

            Console.WriteLine("CSR and PrivateKey generated successfully");

            return true;
        }

        private static async Task<bool> Step2_GetCCSID(CertificateInfo certInfo)
        {
            Console.WriteLine("\nStep 2: Getting Compliance CSID");

            string jsonContent = JsonConvert.SerializeObject(new { csr = certInfo.GeneratedCsr });

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
            _httpClient.DefaultRequestHeaders.Add("OTP", AppConfig.OTP);
            _httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2");

            StringContent content = new(jsonContent, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PostAsync(certInfo.ComplianceCSIDUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Failed to get CCSID");
                return false;
            }

            response.EnsureSuccessStatusCode();

            string resultContent = await response.Content.ReadAsStringAsync();
            ZatcaResultDto zatcaResult = JsonConvert.DeserializeObject<ZatcaResultDto>(resultContent);

            certInfo.CCSIDBinaryToken = zatcaResult.BinarySecurityToken;
            certInfo.CCSIDComplianceRequestId = zatcaResult.RequestID;
            certInfo.CCSIDSecret = zatcaResult.Secret;

            Console.WriteLine("CCSID obtained successfully");
            return true;
        }

        private static async Task<bool> Step3_SendSampleDocuments(CertificateInfo certInfo)
        {
            try
            {

                Console.WriteLine("\nStep 3: Sending Sample Documents\n");

                XmlDocument baseDocument = new() { PreserveWhitespace = true };
                string templatePath = Helpers.GetAbsolutePath(AppConfig.TemplateInvoicePath);

                baseDocument.Load(templatePath);

                (string, string, string)[] documentTypes = new[] {
                                            ("STDSI", "388", "Standard Invoice"),
                                            ("STDCN", "383", "Standard CreditNote"),
                                            ("STDDN", "381", "Standard DebitNote"),
                                            ("SIMSI", "388", "Simplified Invoice"),
                                            ("SIMCN", "383", "Simplified CreditNote"),
                                            ("SIMDN", "381", "Simplified DebitNote")
                                        };

                int icv = 0;
                string pih = "NWZlY2ViNjZmZmM4NmYzOGQ5NTI3ODZjNmQ2OTZjNzljMmRiYzIzOWRkNGU5MWI0NjcyOWQ3M2EyN2ZiNTdlOQ==";

                for (int i = 0; i < documentTypes.Length; i++)
                {
                    (string prefix, string typeCode, string description) = documentTypes[i];
                    icv += 1;

                    bool isSimplified = prefix.StartsWith("SIM");

                    Console.WriteLine($"Processing {description}...");

                    XmlDocument newDoc = Helpers.CreateModifiedInvoiceXml(baseDocument, $"{prefix}-0001", isSimplified ? "0200000" : "0100000", typeCode, icv, pih, description);

                    RequestResult requestResult = Helpers.GenerateRequestApi(newDoc, certInfo, pih, true);

                    ServerResult serverResult = await Helpers.ComplianceCheck(certInfo, requestResult.InvoiceRequest);

                    if (serverResult == null)
                    {
                        Console.WriteLine($"Failed to process {description}: serverResult is null.");
                        return false;
                    }

                    // log Compliance Check

                    JsonSerializerSettings settings = new() { NullValueHandling = NullValueHandling.Ignore };
                    Console.WriteLine($"\n{description}\n\n{JsonConvert.SerializeObject(serverResult, Newtonsoft.Json.Formatting.Indented, settings)}\n\n");


                    string status = isSimplified ? serverResult.ReportingStatus : serverResult.ClearanceStatus;

                    if (status.Contains("REPORTED") || status.Contains("CLEARED"))
                    {
                        pih = requestResult.InvoiceRequest.InvoiceHash;
                        Console.WriteLine($"{description} processed successfully\n\n");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to process {description}: status is {status}\n\n");
                        return false;
                    }

                    await Task.Delay(200);
                }

                return true;
            }
            catch (Exception ex)
            {

                Console.WriteLine($"{ex.Message} \n\n");
                return false;
            }
        }

        private static async Task<bool> Step4_GetPCSID(CertificateInfo certInfo)
        {
            Console.WriteLine("\nStep 4: Getting Production CSID");

            string jsonContent = JsonConvert.SerializeObject(new { compliance_request_id = certInfo.CCSIDComplianceRequestId });

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
            _httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes($"{certInfo.CCSIDBinaryToken}:{certInfo.CCSIDSecret}")));

            StringContent content = new(jsonContent, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PostAsync(certInfo.ProductionCSIDUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Failed to get PCSID");
                return false;
            }

            response.EnsureSuccessStatusCode();

            string resultContent = await response.Content.ReadAsStringAsync();
            ZatcaResultDto zatcaResult = JsonConvert.DeserializeObject<ZatcaResultDto>(resultContent);

            certInfo.PCSIDBinaryToken = zatcaResult.BinarySecurityToken;
            certInfo.PCSIDSecret = zatcaResult.Secret;

            Console.WriteLine($"PCSID obtained successfully\n");
            return true;
        }

    }
}