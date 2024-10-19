// This code demonstrates how to use the Zatca.eInvoice.SDK Library.
// Please make sure we copy ikvm folder from Test folder in Zatca eInvoice SDK to \bin\Debug\net8.0

using Newtonsoft.Json;
using System.Xml;
using ZatcaWithSDK;
using static ZatcaWithSDK.Models;

class Program
{
    private static CertificateInfo certificateInfo;

    static async Task Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Log some information
        Console.WriteLine("\nStarting the console app...");

        // Extract schematrons from Zatca.eInvoiceSDK resources to file
        // Helpers.ExtractSchematrons();

        try
        {
            //Onboarding

            //if (!System.IO.File.Exists(Helpers.GetAbsolutePath(AppConfig.CertificateInfoPath)))
            //{
                Console.WriteLine("\nStarting Onboarding process...");

                OnboardingStep zatcaService = new();
                certificateInfo = await OnboardingStep.DeviceOnboarding();

                Helpers.SerializeToFile<CertificateInfo>(certificateInfo, Helpers.GetAbsolutePath(AppConfig.CertificateInfoPath));

                Console.WriteLine("\nOnboarding process completed successfully.\n");
            //}

            Console.WriteLine("\nStarting Test Approval...\n");

            // Load CertificateInfo from JSON file
            certificateInfo = Helpers.DeserializeFromFile<CertificateInfo>(Helpers.GetAbsolutePath(AppConfig.CertificateInfoPath));

            if (certificateInfo != null)
            {
                XmlDocument document = new() { PreserveWhitespace = true };
                document.Load(Helpers.GetAbsolutePath(AppConfig.TemplateInvoicePath));

                // Process Standard Invoice, Credit Note, and Debit Note
                await ProcessStandardDocuments(document);

                // Process Simplified Invoice, Credit Note, and Debit Note
                await ProcessSimplifiedDocuments(document);

                Console.WriteLine("\n\nALL DONE!\n\n");
            }
            else
            {
                Console.WriteLine("TEST FAILED!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message.ToString(), "An error occurred:");
        }

        Console.ReadLine();
    }

    private static async Task ProcessStandardDocuments(XmlDocument document)
    {
        Console.WriteLine("\n1. Get Standard Invoice Approval\n");

        // Standard Invoice
        XmlDocument newDoc = Helpers.CreateModifiedInvoiceXml(document, "STDSI-001", "0100000", "388", certificateInfo.ICV + 1, certificateInfo.PIH, "");
        Zatca.EInvoice.SDK.Contracts.Models.RequestResult requestResult = Helpers.GenerateRequestApi(newDoc, certificateInfo, certificateInfo.PIH, false);
        ServerResult serverResult = await Helpers.GetApproval(certificateInfo, requestResult.InvoiceRequest, true);
        LogServerResult(serverResult, "Clearance Standard Credit Note");

        // Standard Credit Note
        newDoc = Helpers.CreateModifiedInvoiceXml(document, "STDCN-001", "0100000", "383", certificateInfo.ICV + 1, certificateInfo.PIH, "Standard CreditNote");
        requestResult = Helpers.GenerateRequestApi(newDoc, certificateInfo, certificateInfo.PIH, false);
        serverResult = await Helpers.GetApproval(certificateInfo, requestResult.InvoiceRequest, true);
        LogServerResult(serverResult, "Clearance Standard Credit Note");

        // Standard Debit Note
        newDoc = Helpers.CreateModifiedInvoiceXml(document, "STDDN-001", "0100000", "381", certificateInfo.ICV + 1, certificateInfo.PIH, "Standard DebitNote");
        requestResult = Helpers.GenerateRequestApi(newDoc, certificateInfo, certificateInfo.PIH, false);
        serverResult = await Helpers.GetApproval(certificateInfo, requestResult.InvoiceRequest, true);
        LogServerResult(serverResult, "Clearance Standard Debit Note");
    }

    private static async Task ProcessSimplifiedDocuments(XmlDocument document)
    {
        Console.WriteLine("\n2. Get Simplified Invoice Approval\n");

        // Simplified Invoice

        XmlDocument newDoc = Helpers.CreateModifiedInvoiceXml(document, "SIMSI-001", "0200000", "388", certificateInfo.ICV + 1, certificateInfo.PIH, "");
        Zatca.EInvoice.SDK.Contracts.Models.RequestResult requestResult = Helpers.GenerateRequestApi(newDoc, certificateInfo, certificateInfo.PIH, false);
        ServerResult serverResult = await Helpers.GetApproval(certificateInfo, requestResult.InvoiceRequest, false);
        LogServerResult(serverResult, "Reporting Simplified Invoice");

        // Simplified Credit Note
        newDoc = Helpers.CreateModifiedInvoiceXml(document, "SIMCN-001", "0200000", "383", certificateInfo.ICV + 1, certificateInfo.PIH, "simplified CreditNote");
        requestResult = Helpers.GenerateRequestApi(newDoc, certificateInfo, certificateInfo.PIH, false);
        serverResult = await Helpers.GetApproval(certificateInfo, requestResult.InvoiceRequest, false);
        LogServerResult(serverResult, "Reporting Simplified Credit Note");

        // Simplified Debit Note
        newDoc = Helpers.CreateModifiedInvoiceXml(document, "SIMDN-001", "0200000", "381", certificateInfo.ICV + 1, certificateInfo.PIH, "simplified DebitNote");
        requestResult = Helpers.GenerateRequestApi(newDoc, certificateInfo, certificateInfo.PIH, false);
        serverResult = await Helpers.GetApproval(certificateInfo, requestResult.InvoiceRequest, false);
        LogServerResult(serverResult, "Reporting Simplified Debit Note");
    }

    private static void LogServerResult(ServerResult serverResult, string description)
    {
        JsonSerializerSettings settings = new() { NullValueHandling = NullValueHandling.Ignore };

        if (serverResult != null)
        {
            bool isClearedOrReported = (serverResult.ClearanceStatus?.Contains("CLEARED") == true) ||
                                       (serverResult.ReportingStatus?.Contains("REPORTED") == true);
            bool isNotClearedOrReported = (serverResult.ClearanceStatus?.Contains("NOT") == true) ||
                                          (serverResult.ReportingStatus?.Contains("NOT") == true);

            if (isClearedOrReported || isNotClearedOrReported)
            {
                certificateInfo.ICV += 1;
                certificateInfo.PIH = serverResult.InvoiceHash;
                serverResult.InvoiceHash = null;
                Console.WriteLine($"{description}\n\n{JsonConvert.SerializeObject(serverResult, Newtonsoft.Json.Formatting.Indented, settings)}\n\n");

                //Update Crtificate Info
                Helpers.SerializeToFile<CertificateInfo>(certificateInfo, Helpers.GetAbsolutePath(AppConfig.CertificateInfoPath));
            }
            else
            {
                Console.WriteLine($"{description} was Rejected! \n\n{JsonConvert.SerializeObject(serverResult, Newtonsoft.Json.Formatting.Indented, settings)}\n\n");
            }
        }
        else
        {
            Console.WriteLine($"\n\nError processing {description}\n\n");
        }
    }

}