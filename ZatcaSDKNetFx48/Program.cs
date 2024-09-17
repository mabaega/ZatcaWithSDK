using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Xml;
using Zatca.EInvoice.SDK.Contracts.Models;
using static ZatcaSDKNetFx48.Models;

namespace ZatcaSDKNetFx48
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var zatcaService = new ZatcaService();

            try
            {
                //Onboarding
                Console.WriteLine($"\nI. ONBOARDING PROCESS\n\n");
                var onboardingResult = await zatcaService.OnboardingDevice();


                Console.WriteLine($"\n\nII. ONBOARDING PROCESS\n\n");

                //Send Invoice
                XmlDocument document = new XmlDocument() { PreserveWhitespace = true };
                document.Load(@"..\..\Data\InvSample\CleanSimplified_Invoice.xml");

                XmlDocument newDoc;

                var ICV = "0";
                var PIH = "NWZlY2ViNjZmZmM4NmYzOGQ5NTI3ODZjNmQ2OTZjNzljMmRiYzIzOWRkNGU5MWI0NjcyOWQ3M2EyN2ZiNTdlOQ==";
                RequestResult requestResult;
                ServerResult serverResult;

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };

                Console.WriteLine($"1. Get Standard Invoice Approval\n\n");

                //Standard Invoice
                ICV += ICV;
                newDoc = Helpers.CreateModifiedInvoiceXml(document, "STDSI-001", new Guid().ToString(), "0100000", "388", ICV, PIH, "");
                requestResult = Helpers.GenerateSignedRequestApi(newDoc, onboardingResult.PCSIDBinaryToken, onboardingResult.PrivateKey);
                serverResult = await Helpers.GetApproval(onboardingResult.PCSIDBinaryToken, onboardingResult.PCSIDSecret, requestResult.InvoiceRequest, true);
                if (serverResult != null && !serverResult.ClearanceStatus.Contains("NOT"))
                {
                    PIH = requestResult.InvoiceRequest.InvoiceHash;
                }
                Console.WriteLine($"Clearance Standard Invoice \n{JsonConvert.SerializeObject(serverResult, Newtonsoft.Json.Formatting.Indented, settings)}\n\n");

                //Standard CreditNote
                ICV += ICV;
                newDoc = Helpers.CreateModifiedInvoiceXml(document, "STDCN-001", new Guid().ToString(), "0100000", "383", ICV, PIH, "Standard CreditNote");
                requestResult = Helpers.GenerateSignedRequestApi(newDoc, onboardingResult.PCSIDBinaryToken, onboardingResult.PrivateKey);
                serverResult = await Helpers.GetApproval(onboardingResult.PCSIDBinaryToken, onboardingResult.PCSIDSecret, requestResult.InvoiceRequest, true);
                if (serverResult != null && !serverResult.ClearanceStatus.Contains("NOT"))
                {
                    PIH = requestResult.InvoiceRequest.InvoiceHash;
                }
                Console.WriteLine($"Clearance Standard Credit Note \n{JsonConvert.SerializeObject(serverResult, Newtonsoft.Json.Formatting.Indented, settings)}\n\n");
                //Standard DebitNote
                ICV += ICV;
                newDoc = Helpers.CreateModifiedInvoiceXml(document, "STDDN-001", new Guid().ToString(), "0100000", "381", ICV, PIH, "Standard DebitNote");
                requestResult = Helpers.GenerateSignedRequestApi(newDoc, onboardingResult.PCSIDBinaryToken, onboardingResult.PrivateKey);
                serverResult = await Helpers.GetApproval(onboardingResult.PCSIDBinaryToken, onboardingResult.PCSIDSecret, requestResult.InvoiceRequest, true);
                if (serverResult != null && !serverResult.ClearanceStatus.Contains("NOT"))
                {
                    PIH = requestResult.InvoiceRequest.InvoiceHash;
                }
                Console.WriteLine($"Clearance Standard Debit Note \n{JsonConvert.SerializeObject(serverResult, Newtonsoft.Json.Formatting.Indented, settings)}\n\n");


                Console.WriteLine($"\n\n1. Get Simplified Invoice Approval\n\n");

                //simplified Invoice
                ICV += ICV;
                newDoc = Helpers.CreateModifiedInvoiceXml(document, "SIMSI-001", new Guid().ToString(), "0200000", "388", ICV, PIH, "");
                requestResult = Helpers.GenerateSignedRequestApi(newDoc, onboardingResult.PCSIDBinaryToken, onboardingResult.PrivateKey);
                serverResult = await Helpers.GetApproval(onboardingResult.PCSIDBinaryToken, onboardingResult.PCSIDSecret, requestResult.InvoiceRequest, false);
                if (serverResult != null && !serverResult.ReportingStatus.Contains("NOT"))
                {
                    PIH = requestResult.InvoiceRequest.InvoiceHash;
                }
                Console.WriteLine($"Reporting Simplified Invoice \n{JsonConvert.SerializeObject(serverResult, Newtonsoft.Json.Formatting.Indented, settings)}\n\n");
                //simplified CreditNote
                ICV += ICV;
                newDoc = Helpers.CreateModifiedInvoiceXml(document, "SIMCN-001", new Guid().ToString(), "0200000", "383", ICV, PIH, "simplified CreditNote");
                requestResult = Helpers.GenerateSignedRequestApi(newDoc, onboardingResult.PCSIDBinaryToken, onboardingResult.PrivateKey);
                serverResult = await Helpers.GetApproval(onboardingResult.PCSIDBinaryToken, onboardingResult.PCSIDSecret, requestResult.InvoiceRequest, false);
                if (serverResult != null && !serverResult.ReportingStatus.Contains("NOT"))
                {
                    PIH = requestResult.InvoiceRequest.InvoiceHash;
                }
                Console.WriteLine($"Reporting Simplified Credit Note \n{JsonConvert.SerializeObject(serverResult, Newtonsoft.Json.Formatting.Indented, settings)}\n\n");
                //simplified DebitNote
                ICV += ICV;
                newDoc = Helpers.CreateModifiedInvoiceXml(document, "SIMDN-001", new Guid().ToString(), "0200000", "381", ICV, PIH, "simplified DebitNote");
                requestResult = Helpers.GenerateSignedRequestApi(newDoc, onboardingResult.PCSIDBinaryToken, onboardingResult.PrivateKey);
                serverResult = await Helpers.GetApproval(onboardingResult.PCSIDBinaryToken, onboardingResult.PCSIDSecret, requestResult.InvoiceRequest, false);
                if (serverResult != null && !serverResult.ReportingStatus.Contains("NOT"))
                {
                    PIH = requestResult.InvoiceRequest.InvoiceHash;
                }
                Console.WriteLine($"Reporting Simplified Debit Note \n{JsonConvert.SerializeObject(serverResult, Newtonsoft.Json.Formatting.Indented, settings)}\n\n");

                Console.WriteLine($"\n\nALL DONE!\n\n");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}
