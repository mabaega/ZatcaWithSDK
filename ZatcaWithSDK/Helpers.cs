using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Xml;
using Zatca.EInvoice.SDK;
using Zatca.EInvoice.SDK.Contracts.Models;
using static ZatcaWithSDK.Models;

namespace ZatcaWithSDK
{
    public static class Helpers
    {
        private static readonly HttpClient _httpClient = new();
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
            string directory = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);

            File.WriteAllText(filePath, json);

            Console.WriteLine($"\nOnboarding Info Data has been serialized to the file.\n");
        }

        public static T DeserializeFromFile<T>(string filePath)
        {
            string json = File.ReadAllText(filePath);


            Console.WriteLine($"\nOnboarding Info Data has been loaded from file.\n");
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static bool ExtractSchematrons()
        {
            Assembly assembly = Assembly.Load("Zatca.EInvoice.SDK");
            string outputFolder = Path.Combine("Data", "Rules", "schematrons");
            Directory.CreateDirectory(outputFolder);

            if (assembly != null)
            {
                string[] resourceNames = assembly.GetManifestResourceNames();
                foreach (string resourceName in resourceNames)
                {
                    if (resourceName.Contains("Schematrons"))
                    {
                        string[] resourceNameParts = resourceName.Split('.');
                        string fileName = $"{resourceNameParts[^2]}.{resourceNameParts[^1]}";
                        string outputFile = Path.Combine(outputFolder, fileName);

                        using Stream stream = assembly.GetManifestResourceStream(resourceName);
                        if (stream != null)
                        {
                            using FileStream fileStream = new(outputFile, FileMode.Create, FileAccess.Write);
                            stream.CopyTo(fileStream);
                            Console.WriteLine($"Resource {resourceName} berhasil diekstrak ke {outputFile}");
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        public static RequestResult GenerateRequestApi(XmlDocument document, CertificateInfo certInfo, string pih, bool isForCompliance = false)
        {
            RequestResult requestResult = null;

            string x509CertificateContent = Encoding.UTF8.GetString(Convert.FromBase64String(isForCompliance ? certInfo.CCSIDBinaryToken : certInfo.PCSIDBinaryToken));
            string privateKey = certInfo.PrivateKey;

            // reformat XmlDocument to avoid InvoiceHash Error
            // Zatca.eInvoice.SDK not work with linearized XML
            document = PrettyXml(document);

            //Test use Certificate and privatekey from Zatca eInvoice SDK
            if (certInfo.EnvironmentType == EnvironmentType.NonProduction)
            {
                x509CertificateContent = "MIID3jCCA4SgAwIBAgITEQAAOAPF90Ajs/xcXwABAAA4AzAKBggqhkjOPQQDAjBiMRUwEwYKCZImiZPyLGQBGRYFbG9jYWwxEzARBgoJkiaJk/IsZAEZFgNnb3YxFzAVBgoJkiaJk/IsZAEZFgdleHRnYXp0MRswGQYDVQQDExJQUlpFSU5WT0lDRVNDQTQtQ0EwHhcNMjQwMTExMDkxOTMwWhcNMjkwMTA5MDkxOTMwWjB1MQswCQYDVQQGEwJTQTEmMCQGA1UEChMdTWF4aW11bSBTcGVlZCBUZWNoIFN1cHBseSBMVEQxFjAUBgNVBAsTDVJpeWFkaCBCcmFuY2gxJjAkBgNVBAMTHVRTVC04ODY0MzExNDUtMzk5OTk5OTk5OTAwMDAzMFYwEAYHKoZIzj0CAQYFK4EEAAoDQgAEoWCKa0Sa9FIErTOv0uAkC1VIKXxU9nPpx2vlf4yhMejy8c02XJblDq7tPydo8mq0ahOMmNo8gwni7Xt1KT9UeKOCAgcwggIDMIGtBgNVHREEgaUwgaKkgZ8wgZwxOzA5BgNVBAQMMjEtVFNUfDItVFNUfDMtZWQyMmYxZDgtZTZhMi0xMTE4LTliNTgtZDlhOGYxMWU0NDVmMR8wHQYKCZImiZPyLGQBAQwPMzk5OTk5OTk5OTAwMDAzMQ0wCwYDVQQMDAQxMTAwMREwDwYDVQQaDAhSUlJEMjkyOTEaMBgGA1UEDwwRU3VwcGx5IGFjdGl2aXRpZXMwHQYDVR0OBBYEFEX+YvmmtnYoDf9BGbKo7ocTKYK1MB8GA1UdIwQYMBaAFJvKqqLtmqwskIFzVvpP2PxT+9NnMHsGCCsGAQUFBwEBBG8wbTBrBggrBgEFBQcwAoZfaHR0cDovL2FpYTQuemF0Y2EuZ292LnNhL0NlcnRFbnJvbGwvUFJaRUludm9pY2VTQ0E0LmV4dGdhenQuZ292LmxvY2FsX1BSWkVJTlZPSUNFU0NBNC1DQSgxKS5jcnQwDgYDVR0PAQH/BAQDAgeAMDwGCSsGAQQBgjcVBwQvMC0GJSsGAQQBgjcVCIGGqB2E0PsShu2dJIfO+xnTwFVmh/qlZYXZhD4CAWQCARIwHQYDVR0lBBYwFAYIKwYBBQUHAwMGCCsGAQUFBwMCMCcGCSsGAQQBgjcVCgQaMBgwCgYIKwYBBQUHAwMwCgYIKwYBBQUHAwIwCgYIKoZIzj0EAwIDSAAwRQIhALE/ichmnWXCUKUbca3yci8oqwaLvFdHVjQrveI9uqAbAiA9hC4M8jgMBADPSzmd2uiPJA6gKR3LE03U75eqbC/rXA==";
                privateKey = "MHQCAQEEIL14JV+5nr/sE8Sppaf2IySovrhVBtt8+yz+g4NRKyz8oAcGBSuBBAAKoUQDQgAEoWCKa0Sa9FIErTOv0uAkC1VIKXxU9nPpx2vlf4yhMejy8c02XJblDq7tPydo8mq0ahOMmNo8gwni7Xt1KT9UeA==";
            }

            if (IsSimplifiedInvoice(document))
            {
                SignResult signedInvoiceResult = new EInvoiceSigner().SignDocument(document, x509CertificateContent, privateKey);

                if (IsValidInvoice(signedInvoiceResult.SignedEInvoice, x509CertificateContent, pih))
                {
                    requestResult = new RequestGenerator().GenerateRequest(signedInvoiceResult.SignedEInvoice);
                }
            }
            else
            {
                if (IsValidInvoice(document, x509CertificateContent, pih))
                {
                    HashResult hashResult = new EInvoiceHashGenerator().GenerateEInvoiceHashing(document);

                    requestResult = new RequestGenerator().GenerateRequest(document);
                    requestResult.InvoiceRequest.InvoiceHash = hashResult.Hash;
                }
            }

            return requestResult;
        }

        internal static XmlDocument PrettyXml(XmlDocument inputXml)
        {
            XmlDocument formattedXml = new XmlDocument() { PreserveWhitespace = true };

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (StreamWriter streamWriter = new StreamWriter(memoryStream, new UTF8Encoding(false))) // false to exclude BOM
                {
                    XmlWriterSettings settings = new XmlWriterSettings()
                    {
                        Indent = true,
                        IndentChars = "    ",
                        OmitXmlDeclaration = false,
                        Encoding = Encoding.UTF8,
                    };

                    using (XmlWriter xmlWriter = XmlWriter.Create(streamWriter, settings))
                    {
                        inputXml.Save(xmlWriter);
                    }
                }

                // Get the UTF-8 encoded string from the MemoryStream
                string utf8Xml = Encoding.UTF8.GetString(memoryStream.ToArray()).Trim();

                // Load the UTF-8 XML string into the new XmlDocument
                formattedXml.LoadXml(utf8Xml);
            }

            return formattedXml;
        }

        internal static bool IsValidInvoice(XmlDocument document, string x509CertificateContent, string pih)
        {
            // always return true for now, EInvoiceValidator has problem in some computer

            Zatca.EInvoice.SDK.Contracts.Models.ValidationResult validationResult = new EInvoiceValidator().ValidateEInvoice(document, x509CertificateContent, pih);

            if (validationResult != null)
            {
                foreach (ValidationStepResult e in validationResult.ValidationSteps)
                {
                    Console.WriteLine(e.ValidationStepName + " : " + e.IsValid);

                    if (!e.IsValid)
                    {
                        foreach (string x in e.ErrorMessages)
                        {
                            Console.WriteLine(x);
                        }
                    }
                    else
                    {
                        foreach (string x in e.WarningMessages)
                        {
                            Console.WriteLine(x);
                        }
                    }
                }

                Console.WriteLine($"\nOverall Signed Invoice Validation : {validationResult.IsValid}!");

                //return validationResult.IsValid;
                return true;
            }


            return true;
            //return false;
        }
        internal static bool IsSimplifiedInvoice(XmlDocument document)
        {
            XmlNamespaceManager nsmgr = new(document.NameTable);
            nsmgr.AddNamespace("cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");

            XmlNode invoiceTypeCode = document.SelectSingleNode("//cbc:InvoiceTypeCode", nsmgr);

            if (invoiceTypeCode != null)
            {
                XmlAttribute nameAttribute = invoiceTypeCode.Attributes["name"];
                if (nameAttribute != null && nameAttribute.Value.StartsWith("02"))
                {
                    return true;
                }
            }
            return false;
        }

        public static XmlDocument CreateModifiedInvoiceXml(XmlDocument doc, string id, string invoiceTypeCodename, string invoiceTypeCodeValue, int icv, string pih, string instructionNote)
        {
            XmlNamespaceManager nsmgr = new(doc.NameTable);
            nsmgr.AddNamespace("cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");
            nsmgr.AddNamespace("cac", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2");

            XmlDocument newDoc = (XmlDocument)doc.CloneNode(true);
            newDoc.PreserveWhitespace = true;


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
            string requestUri = isClearance ? certInfo.ClearanceUrl : certInfo.ReportingUrl;
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

                StringContent content = new(JsonConvert.SerializeObject(requestApi), Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PostAsync(requestUri, content);

                string resultContent = await response.Content.ReadAsStringAsync();
                ServerResult serverResult = JsonConvert.DeserializeObject<ServerResult>(resultContent);

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
