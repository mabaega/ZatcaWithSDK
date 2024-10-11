using Newtonsoft.Json;
using System.Collections.Generic;
using Zatca.EInvoice.SDK.Contracts.Models;

namespace NetFx48
{
    public class Models
    {
        public class ZatcaResultDto
        {
            public string RequestID { get; set; }
            public string TokenType { get; set; }
            public string DispositionMessage { get; set; }
            public string BinarySecurityToken { get; set; }
            public string Secret { get; set; }
            public List<string> Errors { get; set; }
        }
        public class CertificateInfo
        {
            public string GeneratedCsr { get; set; }
            public string PrivateKey { get; set; }
            public string CCSIDComplianceRequestId { get; set; }
            public string CCSIDBinaryToken { get; set; }
            public string CCSIDSecret { get; set; }
            public string PCSIDBinaryToken { get; set; }
            public string PCSIDSecret { get; set; }
            public EnvironmentType EnvironmentType { get; set; } = EnvironmentType.NonProduction;

            //Record Last ICV and InvoiceHash
            public int ICV = 0;

            public string PIH = "NWZlY2ViNjZmZmM4NmYzOGQ5NTI3ODZjNmQ2OTZjNzljMmRiYzIzOWRkNGU5MWI0NjcyOWQ3M2EyN2ZiNTdlOQ==";

            public string ReportingUrl => GetUrl("invoices/reporting/single");
            public string ClearanceUrl => GetUrl("invoices/clearance/single");
            public string ComplianceCSIDUrl => GetUrl("compliance");
            public string ComplianceCheckUrl => GetUrl("compliance/invoices");
            public string ProductionCSIDUrl => GetUrl("production/csids");
            private string GetUrl(string endpoint)
            {
                string environment = EnvironmentType switch
                {
                    EnvironmentType.NonProduction => "developer-portal",
                    EnvironmentType.Simulation => "simulation",
                    EnvironmentType.Production => "core",
                    _ => "developer-portal"
                };

                return $"https://gw-fatoora.zatca.gov.sa/e-invoicing/{environment}/{endpoint}";
            }
        }

        public class ServerResult
        {
            [JsonProperty("requestType")]
            public string RequestType { get; set; }

            [JsonProperty("requestUrl")]
            public string RequestUrl { get; set; }

            [JsonProperty("statusCode")]
            public string StatusCode { get; set; }

            [JsonProperty("clearanceStatus")]
            public string ClearanceStatus { get; set; }

            [JsonProperty("reportingStatus")]
            public string ReportingStatus { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("uuid")]
            public string UUID { get; set; }

            [JsonProperty("reasonPhrase")]
            public string ReasonPhrase { get; set; }

            [JsonProperty("isSuccessStatusCode")]
            public string IsSuccessStatusCode { get; set; }

            [JsonProperty("validationResults")]
            public ValidationResult ValidationResults { get; set; }

            [JsonProperty("errorMessages")]
            public List<DetailInfo> ErrorMessages { get; set; }

            [JsonProperty("errors")]
            public List<DetailInfo> Errors { get; set; }

            [JsonProperty("warningMessages")]
            public List<DetailInfo> WarningMessages { get; set; }

            [JsonProperty("warnings")]
            public List<DetailInfo> Warnings { get; set; }

            [JsonProperty("clearedInvoice")]
            public string ClearedInvoice { get; set; }

            [JsonProperty("category")]
            public string Category { get; set; }

            [JsonProperty("code")]
            public string Code { get; set; }

            [JsonProperty("error")]
            public string Error { get; set; }

            [JsonProperty("invoiceHash")]
            public string InvoiceHash { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("qrBuyertStatus")]
            public string QrBuyerStatus { get; set; }

            [JsonProperty("qrSellertStatus")]
            public string QrSellerStatus { get; set; }

            [JsonProperty("timestamp")]
            public string Timestamp { get; set; }

        }
        public class DetailInfo
        {

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("code")]
            public string Code { get; set; }

            [JsonProperty("category")]
            public string Category { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

        }
        public class ValidationResult
        {
            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("infoMessages")]
            public List<DetailInfo> InfoMessages { get; set; }

            [JsonProperty("warningMessages")]
            public List<DetailInfo> WarningMessages { get; set; }

            [JsonProperty("errorMessages")]
            public List<DetailInfo> ErrorMessages { get; set; }

        }

    }
}
