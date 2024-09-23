using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Zatca.EInvoice.SDK.Contracts.Models;
using ZatcaSDKNetFx48;
using static ZatcaSDKNetFx48.Models;


//This code demonstrates how to use the Zatca.e Invoice Library.
public class ZatcaService
{
    public async Task<OnboardingResult> OnboardingDevice()
    {
        var onboardingResult = new OnboardingResult();

        try
        {
            // Step 1: Create CSR

            var csrGenerationDto = new CsrGenerationDto
            (
                 "TST-886431145-399999999900003",
                 "1-TST|2-TST|3-ed22f1d8-e6a2-1118-9b58-d9a8f11e445f",
                 "399999999900003",
                 "Riyadh Branch",
                 "Maximum Speed Tech Supply LTD",
                 "SA",
                 "1100",
                 "RRRD2929",
                 "Supply activities"
            );

            var csrGenerator = new Zatca.EInvoice.SDK.CsrGenerator();
            CsrResult csrResult = csrGenerator.GenerateCsr(csrGenerationDto, EnvironmentType.NonProduction, false);

            onboardingResult.GeneratedCsr = csrResult.Csr;
            onboardingResult.PrivateKey = csrResult.PrivateKey;

            Console.WriteLine($"Step 1\nCSR and PrivateKey Generated Successfully");


            // Step 2: Get CCSID
            const string otp = "12345";
            ZatcaResultDto zatcaResult;

            var jsonContent = JsonConvert.SerializeObject(new { csr = csrResult.Csr });

            using (HttpClient _httpClient = new HttpClient())
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
                _httpClient.DefaultRequestHeaders.Add("OTP", otp);
                _httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2");

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(Helpers.ComplianceCSIDUrl, content);

                response.EnsureSuccessStatusCode();

                var resultContent = await response.Content.ReadAsStringAsync();
                zatcaResult = JsonConvert.DeserializeObject<ZatcaResultDto>(resultContent);
            }

            onboardingResult.CCSIDBinaryToken = zatcaResult.BinarySecurityToken;
            onboardingResult.CCSIDComplianceRequestId = zatcaResult.RequestID;
            onboardingResult.CCSIDSecret = zatcaResult.Secret;

            Console.WriteLine($"Step 2\nGET CCSID Successfully");

            //Step 3. Sending Sample Document
            XmlDocument document = new XmlDocument() { PreserveWhitespace = true };
            document.Load(@"..\..\Data\InvSample\CleanSimplified_Invoice.xml");
            XmlDocument newDoc;

            var ICV = "0";
            var PIH = "NWZlY2ViNjZmZmM4NmYzOGQ5NTI3ODZjNmQ2OTZjNzljMmRiYzIzOWRkNGU5MWI0NjcyOWQ3M2EyN2ZiNTdlOQ==";
            RequestResult requestResult;
            ServerResult serverResult;


            //Standard Invoice
            ICV += ICV;
            newDoc = Helpers.CreateModifiedInvoiceXml(document, "STDSI-001",  "0100000", "388", ICV, PIH, "");
            requestResult = Helpers.GenerateSignedRequestApi(newDoc, onboardingResult.CCSIDBinaryToken, onboardingResult.PrivateKey);
            serverResult = await Helpers.ComplianceCheck(onboardingResult.CCSIDBinaryToken, onboardingResult.CCSIDSecret, requestResult.InvoiceRequest);
            if (serverResult != null && !serverResult.ClearanceStatus.Contains("NOT"))
            {
                PIH = requestResult.InvoiceRequest.InvoiceHash;
            }

            //Standard CreditNote
            ICV += ICV;
            newDoc = Helpers.CreateModifiedInvoiceXml(document, "STDCN-001",  "0100000", "383", ICV, PIH, "Standard CreditNote");
            requestResult = Helpers.GenerateSignedRequestApi(newDoc, onboardingResult.CCSIDBinaryToken, onboardingResult.PrivateKey);
            serverResult = await Helpers.ComplianceCheck(onboardingResult.CCSIDBinaryToken, onboardingResult.CCSIDSecret, requestResult.InvoiceRequest);
            if (serverResult != null && !serverResult.ClearanceStatus.Contains("NOT"))
            {
                PIH = requestResult.InvoiceRequest.InvoiceHash;
            }

            //Standard DebitNote
            ICV += ICV;
            newDoc = Helpers.CreateModifiedInvoiceXml(document, "STDDN-001",  "0100000", "381", ICV, PIH, "Standard DebitNote");
            requestResult = Helpers.GenerateSignedRequestApi(newDoc, onboardingResult.CCSIDBinaryToken, onboardingResult.PrivateKey);
            serverResult = await Helpers.ComplianceCheck(onboardingResult.CCSIDBinaryToken, onboardingResult.CCSIDSecret, requestResult.InvoiceRequest);
            if (serverResult != null && !serverResult.ClearanceStatus.Contains("NOT"))
            {
                PIH = requestResult.InvoiceRequest.InvoiceHash;
            }


            //simplified Invoice
            ICV += ICV;
            newDoc = Helpers.CreateModifiedInvoiceXml(document, "SIMSI-001",  "0200000", "388", ICV, PIH, "");
            requestResult = Helpers.GenerateSignedRequestApi(newDoc, onboardingResult.CCSIDBinaryToken, onboardingResult.PrivateKey);
            serverResult = await Helpers.ComplianceCheck(onboardingResult.CCSIDBinaryToken, onboardingResult.CCSIDSecret, requestResult.InvoiceRequest);
            if (serverResult != null && !serverResult.ReportingStatus.Contains("NOT"))
            {
                PIH = requestResult.InvoiceRequest.InvoiceHash;
            }

            //simplified CreditNote
            ICV += ICV;
            newDoc = Helpers.CreateModifiedInvoiceXml(document, "SIMCN-001",  "0200000", "383", ICV, PIH, "simplified CreditNote");
            requestResult = Helpers.GenerateSignedRequestApi(newDoc, onboardingResult.CCSIDBinaryToken, onboardingResult.PrivateKey);
            serverResult = await Helpers.ComplianceCheck(onboardingResult.CCSIDBinaryToken, onboardingResult.CCSIDSecret, requestResult.InvoiceRequest);
            if (serverResult != null && !serverResult.ReportingStatus.Contains("NOT"))
            {
                PIH = requestResult.InvoiceRequest.InvoiceHash;
            }

            //simplified DebitNote
            ICV += ICV;
            newDoc = Helpers.CreateModifiedInvoiceXml(document, "SIMDN-001",  "0200000", "381", ICV, PIH, "simplified DebitNote");
            requestResult = Helpers.GenerateSignedRequestApi(newDoc, onboardingResult.CCSIDBinaryToken, onboardingResult.PrivateKey);
            serverResult = await Helpers.ComplianceCheck(onboardingResult.CCSIDBinaryToken, onboardingResult.CCSIDSecret, requestResult.InvoiceRequest);
            if (serverResult != null && !serverResult.ReportingStatus.Contains("NOT"))
            {
                PIH = requestResult.InvoiceRequest.InvoiceHash;
            }

            Console.WriteLine($"Step 3\nSend Sample Invoice Successfully");

            // Step 4: Get PCSID

            jsonContent = JsonConvert.SerializeObject(new { compliance_request_id = zatcaResult.RequestID });

            using (HttpClient _httpClient = new HttpClient())
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
                _httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2");
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{zatcaResult.BinarySecurityToken}:{zatcaResult.Secret}")));

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(Helpers.ProductionCSIDUrl, content);

                response.EnsureSuccessStatusCode();

                var resultContent = await response.Content.ReadAsStringAsync();
                zatcaResult = JsonConvert.DeserializeObject<ZatcaResultDto>(resultContent);
            }

            onboardingResult.PCSIDBinaryToken = zatcaResult.BinarySecurityToken;
            onboardingResult.PCSIDSecret = zatcaResult.Secret;

            Console.WriteLine($"Step 4\nGet PCSID Successfully");

            return onboardingResult;

        }

        catch (Exception ex)

        {
            Console.WriteLine($"Error during onboarding: {ex.Message}");
            throw;
        }

    }
}
