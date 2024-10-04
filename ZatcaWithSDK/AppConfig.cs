using Zatca.EInvoice.SDK.Contracts.Models;

namespace ZatcaWithSDK
{
    public static class AppConfig
    {

        // Select EnvironmentType
        public static EnvironmentType EnvironmentType = EnvironmentType.NonProduction;

        // Get OTP from fatoora Portal for Simulation Environment
        public static string OTP = "123456";

        // Change Vat Number with real Vat Number for Simulation Environment
        public static CsrGenerationDto CsrInfoProperties =>
            new CsrGenerationDto
            (
                 "TST-886431145-399999999900003",                       // CommonName
                 "1-TST|2-TST|3-ed22f1d8-e6a2-1118-9b58-d9a8f11e445f",  // SerialNumber
                 "399999999900003",                                     // OrganizationIdentifier
                 "Riyadh Branch",                                       // OrganizationUnitName
                 "Maximum Speed Tech Supply LTD",                       // OrganizationName
                 "SA",                                                  // CountryName
                 "1100",                                                // InvoiceType
                 "RRRD2929",                                            // LocationAddress
                 "Supply activities"                                    // IndustryBusinessCategory
            );


        // Modify this Invoice Template @ <cac:AccountingSupplierParty>,
        // when we use real Vat Number / Simulation Environment
        public static string TemplateInvoicePath { get; set; } = @"Data/InvSample/TemplateInvoice.xml";

        // This file will contain EnvironmentType, Api path and all Certificate Information
        public static string CertificateInfoPath { get; set; } = @"Data/MyCertificate/CertificateInfo.json";
    }
}
