using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Zatca.EInvoice.SDK;
using Zatca.EInvoice.SDK.Contracts.Models;
using static NetFx48.Models;

namespace NetFx48
{
    public class OnboardingStep
    {
        private static readonly HttpClient _httpClient = new();

        public static async Task<CertificateInfo> DeviceOnboarding()
        {
            var certInfo = new CertificateInfo();

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
            var csrGenerator = new CsrGenerator();
            CsrResult csrResult = csrGenerator.GenerateCsr(AppConfig.CsrInfoProperties, AppConfig.EnvironmentType, false);

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

            var jsonContent = JsonConvert.SerializeObject(new { csr = certInfo.GeneratedCsr });

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
            _httpClient.DefaultRequestHeaders.Add("OTP", AppConfig.OTP);
            _httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2");

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(certInfo.ComplianceCSIDUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Failed to get CCSID");
                return false;
            }

            response.EnsureSuccessStatusCode();

            var resultContent = await response.Content.ReadAsStringAsync();
            var zatcaResult = JsonConvert.DeserializeObject<ZatcaResultDto>(resultContent);

            certInfo.CCSIDBinaryToken = zatcaResult.BinarySecurityToken;
            certInfo.CCSIDComplianceRequestId = zatcaResult.RequestID;
            certInfo.CCSIDSecret = zatcaResult.Secret;

            Console.WriteLine("CCSID obtained successfully");
            return true;
        }

        private static async Task<bool> Step3_SendSampleDocuments(CertificateInfo certInfo)
        {
            Console.WriteLine("\nStep 3: Sending Sample Documents\n");

            XmlDocument baseDocument = new() { PreserveWhitespace = true };
            string templatePath = Helpers.GetAbsolutePath(AppConfig.TemplateInvoicePath);

            baseDocument.Load(templatePath);

            var documentTypes = new[] {
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
                var (prefix, typeCode, description) = documentTypes[i];
                icv += 1;

                var isSimplified = prefix.StartsWith("SIM");

                Console.WriteLine($"Processing {description}...");

                var newDoc = Helpers.CreateModifiedInvoiceXml(baseDocument, $"{prefix}-0001", isSimplified ? "0200000" : "0100000", typeCode, icv, pih, description);

                var requestResult = Helpers.GenerateSignedRequestApi(newDoc, certInfo, pih, true);

                var serverResult = await Helpers.ComplianceCheck(certInfo, requestResult.InvoiceRequest);

                if (serverResult == null)
                {
                    Console.WriteLine($"Failed to process {description}: serverResult is null.");
                    return false;
                }

                var status = isSimplified ? serverResult.ReportingStatus : serverResult.ClearanceStatus;

                if (status.Contains("REPORTED") || status.Contains("CLEARED"))
                {
                    pih = requestResult.InvoiceRequest.InvoiceHash;
                    Console.WriteLine($"\n{description} processed successfully\n\n");
                }
                else
                {
                    Console.WriteLine($"Failed to process {description}: status is {status}");
                    return false;
                }

                await Task.Delay(200);
            }

            return true;
        }

        private static async Task<bool> Step4_GetPCSID(CertificateInfo certInfo)
        {
            Console.WriteLine("\nStep 4: Getting Production CSID");

            var jsonContent = JsonConvert.SerializeObject(new { compliance_request_id = certInfo.CCSIDComplianceRequestId });

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
            _httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes($"{certInfo.CCSIDBinaryToken}:{certInfo.CCSIDSecret}")));

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(certInfo.ProductionCSIDUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Failed to get PCSID");
                return false;
            }

            response.EnsureSuccessStatusCode();

            var resultContent = await response.Content.ReadAsStringAsync();
            var zatcaResult = JsonConvert.DeserializeObject<ZatcaResultDto>(resultContent);

            certInfo.PCSIDBinaryToken = zatcaResult.BinarySecurityToken;
            certInfo.PCSIDSecret = zatcaResult.Secret;

            Console.WriteLine($"PCSID obtained successfully\n");
            return true;
        }

    }
}
