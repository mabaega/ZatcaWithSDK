using Zatca.EInvoice.SDK.Contracts.Models;

namespace ZatcaWithSDK
{
    public static class AppConfig
    {
        // Select EnvironmentType
        public static EnvironmentType EnvironmentType = EnvironmentType.NonProduction;

        // Get OTP from fatoora Portal for Simulation Environment
        public static string OTP = "123456";
        public static string TemplateInvoicePath { get; set; } = @"Data/InvSample/TemplateInvoice.xml";

        // This file will contain EnvironmentType, Api path and all Certificate Information
        public static string CertificateInfoPath { get; set; } = @"Data/MyCertificate/CertificateInfo.json";

        // Csr Info Properties Path
        public static string CsrConfigPropertiesPath { get; set; } = @"Data/MyCertificate/csr-config.properties";

        // Load Csr.Config.Properties file and initialize CsrGenerationDto
        public static CsrGenerationDto CsrGenerationDto { get; } = LoadCsrGenerationDto();
        private static CsrGenerationDto LoadCsrGenerationDto()
        {
            Dictionary<string, string> properties = LoadProperties(CsrConfigPropertiesPath);
            return new CsrGenerationDto(
                properties["csr.common.name"],
                properties["csr.serial.number"],
                properties["csr.organization.identifier"],
                properties["csr.organization.unit.name"],
                properties["csr.organization.name"],
                properties["csr.country.name"],
                properties["csr.invoice.type"],
                properties["csr.location.address"],
                properties["csr.industry.business.category"]
            );
        }
        private static Dictionary<string, string> LoadProperties(string filePath)
        {
            Dictionary<string, string> properties = new();
            foreach (string line in System.IO.File.ReadAllLines(filePath))
            {
                if (!string.IsNullOrWhiteSpace(line) && line.Contains('='))
                {
                    string[] parts = line.Split('=', 2);
                    properties[parts[0].Trim()] = parts[1].Trim();
                }
            }
            return properties;
        }
    }
}

