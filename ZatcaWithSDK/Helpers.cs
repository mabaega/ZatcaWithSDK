using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using Zatca.EInvoice.SDK;
using Zatca.EInvoice.SDK.Contracts.Models;
using static ZatcaWithSDK.Models;

namespace ZatcaWithSDK
{
    public static class Helpers
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public static string GetAbsolutePath(string relativePath)
        {
            string baseDirectory = Directory.GetCurrentDirectory();
            string absolutePath = Path.Combine(baseDirectory, relativePath);
            string finalPath = Path.GetFullPath(absolutePath);
            //Console.WriteLine($"{finalPath} : {File.Exists(finalPath)}");
            return finalPath;
        }

        public static void SerializeToFile<T>(T data, string filePath)
        {
            var json = JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filePath, json);
            //Console.WriteLine("Data has been serialized to the file.");
        }

        public static T DeserializeFromFile<T>(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static void CopyDirectory(string sourceDir, string destDir)
        {
            string dirName = Path.GetFileName(sourceDir.TrimEnd(Path.DirectorySeparatorChar));
            string destFolder = Path.Combine(destDir, dirName);
            Directory.CreateDirectory(destFolder);
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destFolder, Path.GetFileName(file));
                File.Copy(file, destFile, true); // Overwrite the file if it already exists
            }

            foreach (string subdir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destFolder, Path.GetFileName(subdir));
                Directory.CreateDirectory(destSubDir);
                CopyDirectory(subdir, destSubDir);
            }
        }

        public static RequestResult GenerateSignedRequestApi(XmlDocument document, string csidBinaryToken, string privateKey, string pih)
        {
            string x509CertificateContent = Encoding.UTF8.GetString(Convert.FromBase64String(csidBinaryToken));
            SignResult signedInvoiceResult = new EInvoiceSigner().SignDocument(document, x509CertificateContent, privateKey);

            // Validate Signed Invoice *** just test ***
            EInvoiceValidator eInvoiceValidator = new();
            var validationResult = eInvoiceValidator.ValidateEInvoice(signedInvoiceResult.SignedEInvoice, x509CertificateContent, pih);
            if (validationResult != null)
            {
                foreach (var e in validationResult.ValidationSteps)
                {
                    Console.WriteLine(e.ValidationStepName + " : " + e.IsValid);
                    if (!e.IsValid)
                    {
                        foreach (var x in e.ErrorMessages)
                        {
                            Console.WriteLine(x);
                        }
                    }
                    else
                    {
                        foreach (var x in e.WarningMessages)
                        {
                            Console.WriteLine(x);
                        }
                    }

                }
                Console.WriteLine($"Overall Signed Invoice Validation : {validationResult.IsValid}!");
            }

            return new RequestGenerator().GenerateRequest(signedInvoiceResult.SignedEInvoice);
        }

        public static XmlDocument CreateModifiedInvoiceXml(XmlDocument doc, string id, string invoiceTypeCodename, string invoiceTypeCodeValue, int icv, string pih, string instructionNote)
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

            // Corrected XPath expression
            XmlNode additionalReferenceNode = newDoc.SelectSingleNode("//cac:AdditionalDocumentReference[cbc:ID='ICV']/cbc:UUID", nsmgr);
            if (additionalReferenceNode != null)
            {
                additionalReferenceNode.InnerText = icv.ToString();
            }
            else
            {
                Console.WriteLine("UUID node not found for ICV.");
            }

            XmlNode pihNode = newDoc.SelectSingleNode("//cac:AdditionalDocumentReference[cbc:ID='PIH']/cac:Attachment/cbc:EmbeddedDocumentBinaryObject", nsmgr);
            if (pihNode != null)
            {
                pihNode.InnerText = pih;
            }
            else
            {
                Console.WriteLine("EmbeddedDocumentBinaryObject node not found for PIH.");
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

        public static async Task<ServerResult> ComplianceCheck(CertificateInfo certInfo, InvoiceRequest requestApi)
        {
            return await PerformApiRequest(certInfo.ComplianceCheckUrl, requestApi, certInfo.CCSIDBinaryToken, certInfo.CCSIDSecret, "Compliance Check");
        }

        public static async Task<ServerResult> GetApproval(CertificateInfo certInfo, InvoiceRequest requestApi, bool isClearance)
        {
            var requestUri = isClearance ? certInfo.ClearanceUrl : certInfo.ReportingUrl;
            return await PerformApiRequest(requestUri, requestApi, certInfo.PCSIDBinaryToken, certInfo.PCSIDSecret, isClearance ? "Clearance" : "Reporting", isClearance);
        }

        private static async Task<ServerResult> PerformApiRequest(string requestUri, InvoiceRequest requestApi, string token, string secret, string requestType, bool isClearance = false)
        {

            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
                _httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2");
                if (isClearance)
                {
                    _httpClient.DefaultRequestHeaders.Add("Clearance-Status", "1");
                }
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{token}:{secret}")));

                var content = new StringContent(JsonConvert.SerializeObject(requestApi), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(requestUri, content);

                var resultContent = await response.Content.ReadAsStringAsync();
                var serverResult = JsonConvert.DeserializeObject<ServerResult>(resultContent);

                serverResult.StatusCode = $"{(int)response.StatusCode}-{response.StatusCode}";
                serverResult.RequestType = requestType;
                serverResult.RequestUrl = requestUri;
                serverResult.InvoiceHash = requestApi.InvoiceHash;

                return serverResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during {requestType}: {ex.Message}");
                throw;
            }
        }
    }
}
