using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Zatca.EInvoice.SDK;
using Zatca.EInvoice.SDK.Contracts.Models;
using static ZatcaSDKNetFx48.Models;

namespace ZatcaSDKNetFx48
{
    public class Helpers
    {
        public const string ComplianceCSIDUrl = "https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal/compliance";
        public const string ProductionCSIDUrl = "https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal/production/csids";
        public const string ComplianceCheckUrl = "https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal/compliance/invoices";
        public const string ReportingUrl = "https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal/invoices/reporting/single";
        public const string ClearanceUrl = "https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal/invoices/clearance/single";

        public static void SerializeToFile(OnboardingResult onboardingResult, string filePath)
        {
            var json = JsonConvert.SerializeObject(onboardingResult, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filePath, json);
            Console.WriteLine("Data has been serialized to the file.");
        }

        public static OnboardingResult DeserializeFromFile(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<OnboardingResult>(json);
        }


        public static RequestResult GenerateSignedRequestApi(XmlDocument document, string csidBynaryToken, string privateKey)
        {

            string ccsidBinaryTokenString = Encoding.UTF8.GetString(Convert.FromBase64String(csidBynaryToken));
            SignResult signedInvoiceResult = new EInvoiceSigner().SignDocument(document, ccsidBinaryTokenString, privateKey);
            RequestResult requestResult = new RequestGenerator().GenerateRequest(signedInvoiceResult.SignedEInvoice);

            return requestResult;
        }

        public static XmlDocument CreateModifiedInvoiceXml(XmlDocument doc, string id, string invoiceTypeCodename, string invoiceTypeCodeValue, string icv, string pih, string instructionNote)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");
            nsmgr.AddNamespace("cac", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2");

            XmlDocument newDoc = (XmlDocument)doc.CloneNode(true);

            Guid newGuid = Guid.NewGuid();
            string guidString = newGuid.ToString();

            XmlNode idNode = newDoc.SelectSingleNode("//cbc:ID", nsmgr);
            if (idNode != null)
            {
                idNode.InnerText = id;
            }

            XmlNode uuidNode = newDoc.SelectSingleNode("//cbc:UUID", nsmgr);
            if (uuidNode != null)
            {
                uuidNode.InnerText = guidString;
            }

            XmlNode invoiceTypeCodeNode = newDoc.SelectSingleNode("//cbc:InvoiceTypeCode", nsmgr);
            if (invoiceTypeCodeNode != null)
            {
                XmlAttribute nameAttr = invoiceTypeCodeNode.Attributes["name"];
                if (nameAttr != null)
                {
                    nameAttr.Value = invoiceTypeCodename;
                }
                invoiceTypeCodeNode.InnerText = invoiceTypeCodeValue;
            }

            XmlNode additionalReferenceNode = newDoc.SelectSingleNode("//cac:AdditionalReference[cac:ID='ICV']/cbc:UUID", nsmgr);
            if (additionalReferenceNode != null)
            {
                additionalReferenceNode.InnerText = icv;
            }

            XmlNode pihNode = newDoc.SelectSingleNode("//cac:AdditionalDocumentReference[cbc:ID='PIH']/cac:Attachment/cbc:EmbeddedDocumentBinaryObject", nsmgr);
            if (pihNode != null)
            {
                pihNode.InnerText = pih;
            }


            if (!string.IsNullOrEmpty(instructionNote))
            {
                // Add the InstructionNote element
                XmlNode paymentMeansNode = newDoc.SelectSingleNode("//cac:PaymentMeans", nsmgr);
                if (paymentMeansNode != null)
                {
                    XmlElement instructionNoteElement = newDoc.CreateElement("cbc", "InstructionNote", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");
                    instructionNoteElement.InnerText = instructionNote;
                    paymentMeansNode.AppendChild(instructionNoteElement);
                }
            }
            else
            {
                // Remove BillingReference elements
                XmlNodeList billingReferenceNodes = newDoc.SelectNodes("//cac:BillingReference", nsmgr);
                foreach (XmlNode billingReferenceNode in billingReferenceNodes)
                {
                    billingReferenceNode.ParentNode.RemoveChild(billingReferenceNode);
                }
            }

            return newDoc;
        }



        public static async Task<ServerResult> ComplianceCheck(string ccsidBinaryToken, string ccsidSecret, InvoiceRequest requestApi)
        {
            try
            {
                ServerResult serverResult;

                using (HttpClient _httpClient = new HttpClient())
                {

                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
                    _httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2");
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(Encoding.ASCII.GetBytes($"{ccsidBinaryToken}:{ccsidSecret}")));

                    var content = new StringContent(JsonConvert.SerializeObject(requestApi), Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync(ComplianceCheckUrl, content);

                    var resultContent = await response.Content.ReadAsStringAsync();

                    serverResult = JsonConvert.DeserializeObject<ServerResult>(resultContent);
                    serverResult.StatusCode = $"{(int)response.StatusCode}-{response.StatusCode}";
                }

                serverResult.RequestType = "Compliance Check";
                serverResult.RequestUrl = ComplianceCheckUrl;

                return serverResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during compliance check: {ex.Message}");
                throw;
            }
        }

        public static async Task<ServerResult> GetApproval(string pcsidBinaryToken, string pcsidSecret, InvoiceRequest requestApi, bool IsClearance)
        {
            try
            {
                var requestUri = IsClearance ? ClearanceUrl : ReportingUrl;

                ServerResult serverResult;

                using (HttpClient _httpClient = new HttpClient())
                {

                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
                    _httpClient.DefaultRequestHeaders.Add("Clearance-Status", IsClearance ? "1" : "0");
                    _httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2");
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(Encoding.ASCII.GetBytes($"{pcsidBinaryToken}:{pcsidSecret}")));

                    var content = new StringContent(JsonConvert.SerializeObject(requestApi), Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync(requestUri, content);

                    var resultContent = await response.Content.ReadAsStringAsync();
                    serverResult = JsonConvert.DeserializeObject<ServerResult>(resultContent);

                    serverResult.StatusCode = $"{(int)response.StatusCode}-{response.StatusCode}";
                }

                serverResult.RequestType = "Invoice Reporting";
                serverResult.RequestUrl = ComplianceCheckUrl;

                return serverResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during compliance check: {ex.Message}");
                throw;
            }
        }
    }
}
