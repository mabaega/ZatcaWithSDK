using Newtonsoft.Json;
using System.Reflection;
using System.Xml;
using static ZatcaWithSDK.Models;

namespace ZatcaWithSDK
{
    class Program
    {
        static async Task Main(string[] args)
        {
           
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var zatcaService = new ZatcaService();

            try
            {
                var onboardingJsonPath = Helpers.GetFullPath(@"Data/MyCertificate/ObboardingInfo.json");


                //Commented this block after 1st run with successfully onboarding

                #region "Onboarding" 

                Console.WriteLine($"\nI. ONBOARDING PROCESS\n\n");
                var onboardingResult = await zatcaService.OnboardingDevice();
                //Save OnboardingInfo
                Helpers.SerializeToFile(onboardingResult, onboardingJsonPath);

                #endregion


                Console.WriteLine($"\n\nII. APPROVAL PROCESS\n\n");

                //Load CertificateInfo
                OnboardingResult certificateInfo = Helpers.DeserializeFromFile(onboardingJsonPath);

                XmlDocument document = new() { PreserveWhitespace = true };
                document.Load(Helpers.GetFullPath(@"Data/InvSample/CleanSimplified_Invoice.xml"));

                XmlDocument newDoc;

                var ICV = "0";
                var PIH = "NWZlY2ViNjZmZmM4NmYzOGQ5NTI3ODZjNmQ2OTZjNzljMmRiYzIzOWRkNGU5MWI0NjcyOWQ3M2EyN2ZiNTdlOQ==";
                Zatca.EInvoice.SDK.Contracts.Models.RequestResult requestResult;
                ServerResult serverResult;

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };

                Console.WriteLine($"1. Get Standard Invoice Approval\n\n");

                //Standard Invoice
                ICV += ICV;
                newDoc = Helpers.CreateModifiedInvoiceXml(document, "STDSI-001",  "0100000", "388", ICV, PIH, "");
                requestResult = Helpers.GenerateSignedRequestApi(newDoc, certificateInfo.PCSIDBinaryToken, certificateInfo.PrivateKey);
                serverResult = await Helpers.GetApproval(certificateInfo.PCSIDBinaryToken, certificateInfo.PCSIDSecret, requestResult.InvoiceRequest, true);
                if (serverResult != null && !serverResult.ClearanceStatus.Contains("NOT"))
                {
                    PIH = requestResult.InvoiceRequest.InvoiceHash;
                }
                Console.WriteLine($"Clearance Standard Invoice \n{JsonConvert.SerializeObject(serverResult, Newtonsoft.Json.Formatting.Indented, settings)}\n\n");

                //Standard CreditNote
                ICV += ICV;
                newDoc = Helpers.CreateModifiedInvoiceXml(document, "STDCN-001",  "0100000", "383", ICV, PIH, "Standard CreditNote");
                requestResult = Helpers.GenerateSignedRequestApi(newDoc, certificateInfo.PCSIDBinaryToken, certificateInfo.PrivateKey);
                serverResult = await Helpers.GetApproval(certificateInfo.PCSIDBinaryToken, certificateInfo.PCSIDSecret, requestResult.InvoiceRequest, true);
                if (serverResult != null && !serverResult.ClearanceStatus.Contains("NOT"))
                {
                    PIH = requestResult.InvoiceRequest.InvoiceHash;
                }
                Console.WriteLine($"Clearance Standard Credit Note \n{JsonConvert.SerializeObject(serverResult, Newtonsoft.Json.Formatting.Indented, settings)}\n\n");

                //Standard DebitNote
                ICV += ICV;
                newDoc = Helpers.CreateModifiedInvoiceXml(document, "STDDN-001",  "0100000", "381", ICV, PIH, "Standard DebitNote");
                requestResult = Helpers.GenerateSignedRequestApi(newDoc, certificateInfo.PCSIDBinaryToken, certificateInfo.PrivateKey);
                serverResult = await Helpers.GetApproval(certificateInfo.PCSIDBinaryToken, certificateInfo.PCSIDSecret, requestResult.InvoiceRequest, true);
                if (serverResult != null && !serverResult.ClearanceStatus.Contains("NOT"))
                {
                    PIH = requestResult.InvoiceRequest.InvoiceHash;
                }
                Console.WriteLine($"Clearance Standard Debit Note \n{JsonConvert.SerializeObject(serverResult, Newtonsoft.Json.Formatting.Indented, settings)}\n\n");


                Console.WriteLine($"\n\n2. Get Simplified Invoice Approval\n\n");

                //simplified Invoice
                ICV += ICV;
                newDoc = Helpers.CreateModifiedInvoiceXml(document, "SIMSI-001",  "0200000", "388", ICV, PIH, "");
                requestResult = Helpers.GenerateSignedRequestApi(newDoc, certificateInfo.PCSIDBinaryToken, certificateInfo.PrivateKey);
                serverResult = await Helpers.GetApproval(certificateInfo.PCSIDBinaryToken, certificateInfo.PCSIDSecret, requestResult.InvoiceRequest, false);
                if (serverResult != null && !serverResult.ReportingStatus.Contains("NOT"))
                {
                    PIH = requestResult.InvoiceRequest.InvoiceHash;
                }
                Console.WriteLine($"Reporting Simplified Invoice \n{JsonConvert.SerializeObject(serverResult, Newtonsoft.Json.Formatting.Indented, settings)}\n\n");

                //simplified CreditNote
                string pihforvalidation = string.Empty;

                ICV += ICV;
                newDoc = Helpers.CreateModifiedInvoiceXml(document, "SIMCN-001",  "0200000", "383", ICV, PIH, "simplified CreditNote");
                requestResult = Helpers.GenerateSignedRequestApi(newDoc, certificateInfo.PCSIDBinaryToken, certificateInfo.PrivateKey);
                serverResult = await Helpers.GetApproval(certificateInfo.PCSIDBinaryToken, certificateInfo.PCSIDSecret, requestResult.InvoiceRequest, false);
                if (serverResult != null && !serverResult.ReportingStatus.Contains("NOT"))
                {
                    PIH = requestResult.InvoiceRequest.InvoiceHash;
                    pihforvalidation = PIH;
                }
                Console.WriteLine($"Reporting Simplified Credit Note \n{JsonConvert.SerializeObject(serverResult, Newtonsoft.Json.Formatting.Indented, settings)}\n\n");

                //simplified DebitNote
                ICV += ICV;
                newDoc = Helpers.CreateModifiedInvoiceXml(document, "SIMDN-001",  "0200000", "381", ICV, PIH, "simplified DebitNote");
                requestResult = Helpers.GenerateSignedRequestApi(newDoc, certificateInfo.PCSIDBinaryToken, certificateInfo.PrivateKey);
                serverResult = await Helpers.GetApproval(certificateInfo.PCSIDBinaryToken, certificateInfo.PCSIDSecret, requestResult.InvoiceRequest, false);
                if (serverResult != null && !serverResult.ReportingStatus.Contains("NOT"))
                {
                    PIH = requestResult.InvoiceRequest.InvoiceHash;

                }
                Console.WriteLine($"Reporting Simplified Debit Note \n{JsonConvert.SerializeObject(serverResult, Newtonsoft.Json.Formatting.Indented, settings)}\n\n");

                Console.WriteLine($"\n\nALL DONE!\n\n");


                ////Test eInvoice SDK Validator
                //string cert = Encoding.UTF8.GetString(Convert.FromBase64String(certificateInfo.PCSIDBinaryToken));
                //XmlDocument zdocument = new XmlDocument() { PreserveWhitespace = true };
                //zdocument.LoadXml(Encoding.UTF8.GetString(Convert.FromBase64String(requestResult.InvoiceRequest.Invoice)));

                //Zatca.EInvoice.SDK.Contracts.Models.ValidationResult result = new EInvoiceValidator().ValidateEInvoice(zdocument, cert, pihforvalidation);
                //foreach (var v in result.ValidationSteps)
                //{
                //    Console.WriteLine(v.ValidationStepName);
                //    Console.WriteLine(v.IsValid);
                //    foreach (var v2 in v.ErrorMessages)
                //    {
                //        Console.WriteLine(v2.ToString());
                //    }

                //    Console.WriteLine();
                //}

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}
