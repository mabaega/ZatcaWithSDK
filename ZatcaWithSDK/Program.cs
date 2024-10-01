// This code demonstrates how to use the Zatca.eInvoice.SDK Library.
// Please make sure we copy ikvm folder from Test folder in Zatca eInvoice SDK to \bin\Debug\net8.0

using System.Xml;
using Newtonsoft.Json;
using static ZatcaWithSDK.Models;
using ZatcaWithSDK;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

class Program
{
    private static CertificateInfo certificateInfo;

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        //await Class1.Test();
        
        //return;


        // Handle Schematrons error on validation method

        //string sourceDir = Helpers.GetAbsolutePath(@"../../../ikvm");
        //string destDir = AppDomain.CurrentDomain.BaseDirectory;
        //Helpers.CopyDirectory(sourceDir, destDir);
        //Console.WriteLine("Copy operation completed.");

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning) 
                .AddFilter("System", LogLevel.Warning) 
                .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug)
                .AddConsole(options =>
                {
                    options.FormatterName = "custom";
                })
                .AddConsoleFormatter<CustomConsoleFormatter, SimpleConsoleFormatterOptions>();
        });

        // Create a logger for the Program class
        ILogger logger = loggerFactory.CreateLogger<Program>();

        // Log some information
        logger.LogInformation("\nStarting the console app...");

        // Create an instance of ZatcaService, passing the logger
        var zatcaService = new ZatcaService(new HttpClient(), logger);

        try
        {
            // Onboarding
            logger.LogInformation("\nStarting Onboarding process...");
            certificateInfo = await zatcaService.DeviceOnboarding();
            Helpers.SerializeToFile<CertificateInfo>(certificateInfo, Helpers.GetAbsolutePath(AppConfig.CertificateInfoPath));
            logger.LogInformation("\nOnboarding process completed successfully.\n");

            logger.LogInformation("\nStarting Test Approval...\n");

            // Load CertificateInfo from JSON file
            certificateInfo = Helpers.DeserializeFromFile<CertificateInfo>(Helpers.GetAbsolutePath(AppConfig.CertificateInfoPath));

            if (certificateInfo != null)
            {
                XmlDocument document = new() { PreserveWhitespace = true };
                document.Load(Helpers.GetAbsolutePath(AppConfig.TemplateInvoicePath));

                // Process Standard Invoice, Credit Note, and Debit Note
                await ProcessStandardDocuments(document, logger);

                // Process Simplified Invoice, Credit Note, and Debit Note
                await ProcessSimplifiedDocuments(document, logger);

                logger.LogInformation("\n\nALL DONE!\n\n");
            }
            else
            {
                logger.LogError("TEST FAILED!");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred:");
        }
    }

    private static async Task ProcessStandardDocuments(XmlDocument document, ILogger logger)
    {
        logger.LogInformation("\n1. Get Standard Invoice Approval\n");

        // Standard Invoice
        var newDoc = Helpers.CreateModifiedInvoiceXml(document, "STDSI-001", "0100000", "388", certificateInfo.ICV + 1, certificateInfo.PIH, "");
        var requestResult = Helpers.GenerateSignedRequestApi(newDoc, certificateInfo.PCSIDBinaryToken, certificateInfo.PrivateKey, certificateInfo.PIH);
        var serverResult = await Helpers.GetApproval(certificateInfo, requestResult.InvoiceRequest, true);
        LogServerResult(serverResult, "Clearance Standard Credit Note", logger);

        // Standard Credit Note
        newDoc = Helpers.CreateModifiedInvoiceXml(document, "STDCN-001", "0100000", "383", certificateInfo.ICV + 1, certificateInfo.PIH, "Standard CreditNote");
        requestResult = Helpers.GenerateSignedRequestApi(newDoc, certificateInfo.PCSIDBinaryToken, certificateInfo.PrivateKey, certificateInfo.PIH);
        serverResult = await Helpers.GetApproval(certificateInfo, requestResult.InvoiceRequest, true);
        LogServerResult(serverResult,  "Clearance Standard Credit Note", logger);

        // Standard Debit Note
        newDoc = Helpers.CreateModifiedInvoiceXml(document, "STDDN-001", "0100000", "381", certificateInfo.ICV + 1, certificateInfo.PIH, "Standard DebitNote");
        requestResult = Helpers.GenerateSignedRequestApi(newDoc, certificateInfo.PCSIDBinaryToken, certificateInfo.PrivateKey, certificateInfo.PIH);
        serverResult = await Helpers.GetApproval(certificateInfo, requestResult.InvoiceRequest, true);
        LogServerResult(serverResult, "Clearance Standard Debit Note", logger);
    }

    private static async Task ProcessSimplifiedDocuments(XmlDocument document, ILogger logger)
    {
        logger.LogInformation("\n2. Get Simplified Invoice Approval\n");

        // Simplified Invoice

        var newDoc = Helpers.CreateModifiedInvoiceXml(document, "SIMSI-001", "0200000", "388", certificateInfo.ICV + 1, certificateInfo.PIH, "");
        var requestResult = Helpers.GenerateSignedRequestApi(newDoc, certificateInfo.PCSIDBinaryToken, certificateInfo.PrivateKey, certificateInfo.PIH);
        var serverResult = await Helpers.GetApproval(certificateInfo, requestResult.InvoiceRequest, false);
        LogServerResult(serverResult, "Reporting Simplified Invoice", logger);

        // Simplified Credit Note
        newDoc = Helpers.CreateModifiedInvoiceXml(document, "SIMCN-001", "0200000", "383", certificateInfo.ICV + 1, certificateInfo.PIH, "simplified CreditNote");
        requestResult = Helpers.GenerateSignedRequestApi(newDoc, certificateInfo.PCSIDBinaryToken, certificateInfo.PrivateKey, certificateInfo.PIH);
        serverResult = await Helpers.GetApproval(certificateInfo, requestResult.InvoiceRequest, false);
        LogServerResult(serverResult, "Reporting Simplified Credit Note", logger);

        // Simplified Debit Note
        newDoc = Helpers.CreateModifiedInvoiceXml(document, "SIMDN-001", "0200000", "381", certificateInfo.ICV +1, certificateInfo.PIH, "simplified DebitNote");
        requestResult = Helpers.GenerateSignedRequestApi(newDoc, certificateInfo.PCSIDBinaryToken, certificateInfo.PrivateKey, certificateInfo.PIH);
        serverResult = await Helpers.GetApproval(certificateInfo, requestResult.InvoiceRequest, false);
        LogServerResult(serverResult, "Reporting Simplified Debit Note", logger);
    }

    private static void LogServerResult(ServerResult serverResult, string description, ILogger logger)
    {
        var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

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
                logger.LogInformation($"{description}\n\n{JsonConvert.SerializeObject(serverResult, Newtonsoft.Json.Formatting.Indented, settings)}\n\n");
                
                //Update Crtificate Info
                Helpers.SerializeToFile<CertificateInfo>(certificateInfo, Helpers.GetAbsolutePath(AppConfig.CertificateInfoPath));
            }
            else
            {
                logger.LogInformation($"{description} was Rejected! \n\n{JsonConvert.SerializeObject(serverResult, Newtonsoft.Json.Formatting.Indented, settings)}\n\n");
            }
        }
        else
        {
            logger.LogError($"\n\nError processing {description}\n\n");
        }
    }

}