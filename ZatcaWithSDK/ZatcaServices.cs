//This code demonstrates how to use the Zatca.e Invoice Library for Onboarding.

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using Zatca.EInvoice.SDK;
using Zatca.EInvoice.SDK.Contracts.Models;
using ZatcaWithSDK;
using static ZatcaWithSDK.Models;

public class ZatcaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public ZatcaService(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CertificateInfo> DeviceOnboarding()
    {
        var certInfo = new CertificateInfo();

        try
        {
            await Step1_GenerateCSR(certInfo);
            await Step2_GetCCSID(certInfo);
            await Step3_SendSampleDocuments(certInfo);
            await Step4_GetPCSID(certInfo);

            return certInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error during onboarding: {ex.Message}");
            throw;
        }
    }

    private async Task Step1_GenerateCSR(CertificateInfo certInfo)
    {
        _logger.LogInformation("\nStep 1: Generating CSR and PrivateKey");
        var csrGenerator = new CsrGenerator();
        CsrResult csrResult = csrGenerator.GenerateCsr(AppConfig.CsrInfoProperties, AppConfig.EnvironmentType, false);

        if (!csrResult.IsValid)
        {
            throw new Exception("Failed to generate CSR");
        }

        certInfo.GeneratedCsr = csrResult.Csr;
        certInfo.PrivateKey = csrResult.PrivateKey;
        _logger.LogInformation("CSR and PrivateKey generated successfully");
    }

    private async Task Step2_GetCCSID(CertificateInfo certInfo)
    {
        _logger.LogInformation("\nStep 2: Getting Compliance CSID");

        var jsonContent = JsonConvert.SerializeObject(new { csr = certInfo.GeneratedCsr });

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
        _httpClient.DefaultRequestHeaders.Add("OTP", AppConfig.OTP);
        _httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2");

        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(certInfo.ComplianceCSIDUrl, content);

        response.EnsureSuccessStatusCode();

        var resultContent = await response.Content.ReadAsStringAsync();
        var zatcaResult = JsonConvert.DeserializeObject<ZatcaResultDto>(resultContent);

        certInfo.CCSIDBinaryToken = zatcaResult.BinarySecurityToken;
        certInfo.CCSIDComplianceRequestId = zatcaResult.RequestID;
        certInfo.CCSIDSecret = zatcaResult.Secret;

        _logger.LogInformation("CCSID obtained successfully");
    }

    private async Task Step3_SendSampleDocuments(CertificateInfo certInfo)
    {
        _logger.LogInformation("\nStep 3: Sending Sample Documents");

        XmlDocument baseDocument = new() { PreserveWhitespace = true };
        baseDocument.Load(Helpers.GetAbsolutePath(AppConfig.TemplateInvoicePath));

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

        foreach (var (prefix, typeCode, description) in documentTypes)
        {
            icv += 1;
            var isSimplified = prefix.StartsWith("SIM");
            var newDoc = Helpers.CreateModifiedInvoiceXml(baseDocument, $"{prefix}-001", isSimplified ? "0200000" : "0100000", typeCode, icv, pih, description);

            var requestResult = Helpers.GenerateSignedRequestApi(newDoc, certInfo.CCSIDBinaryToken, certInfo.PrivateKey);

            var serverResult = await Helpers.ComplianceCheck(certInfo, requestResult.InvoiceRequest);

            if (serverResult == null || (isSimplified ? serverResult.ReportingStatus : serverResult.ClearanceStatus).Contains("NOT"))
            {
                throw new Exception($"Failed to process {description}");
            }

            pih = requestResult.InvoiceRequest.InvoiceHash;
            _logger.LogInformation($"\n{description} processed successfully");
        }
    }

    private async Task Step4_GetPCSID(CertificateInfo certInfo)
    {
        _logger.LogInformation("\nStep 4: Getting Production CSID");

        var jsonContent = JsonConvert.SerializeObject(new { compliance_request_id = certInfo.CCSIDComplianceRequestId });

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
        _httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"{certInfo.CCSIDBinaryToken}:{certInfo.CCSIDSecret}")));

        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(certInfo.ProductionCSIDUrl, content);
        response.EnsureSuccessStatusCode();

        var resultContent = await response.Content.ReadAsStringAsync();
        var zatcaResult = JsonConvert.DeserializeObject<ZatcaResultDto>(resultContent);

        certInfo.PCSIDBinaryToken = zatcaResult.BinarySecurityToken;
        certInfo.PCSIDSecret = zatcaResult.Secret;

        _logger.LogInformation($"PCSID obtained successfully\n");

    }

}
