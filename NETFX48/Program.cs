using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using Zatca.EInvoice.SDK;
using Zatca.EInvoice.SDK.Contracts.Models;

namespace DotNetFXSDK
{
    internal class Program
    {
        static void Main(string[] args)
        {
            CultureInfo culture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            string certificateContent = File.ReadAllText(@"..\..\Data\Certificates\cert.pem");
            string privateKeyContent = File.ReadAllText(@"..\..\Data\Certificates\ec-secp256k1-priv-key.pem");

            XmlDocument eInvoice = new XmlDocument();
            eInvoice.Load(@"..\..\Data\Samples\Simplified\Invoice\Simplified_Invoice.xml");

            EInvoiceSigner eInvoiceSigner = new EInvoiceSigner();
            SignResult signResult = eInvoiceSigner.SignDocument(eInvoice, certificateContent, privateKeyContent);
            
            signResult.SaveSignedEInvoice(@"c:\tmp\Simplified_Invoice1.xml");
            Console.WriteLine(signResult.SignedEInvoice.InnerXml.ToString());

            ShowSignResult(signResult);

            if ((signResult != null) && signResult.IsValid)
            {
                EInvoiceValidator EInvoiceValidator = new EInvoiceValidator();
                ValidationResult ValidationResult = EInvoiceValidator.ValidateEInvoice(signResult.SignedEInvoice, certificateContent, "NWZlY2ViNjZmZmM4NmYzOGQ5NTI3ODZjNmQ2OTZjNzljMmRiYzIzOWRkNGU5MWI0NjcyOWQ3M2EyN2ZiNTdlOQ==");

                ShowValidationResult(ValidationResult);
                Console.WriteLine();

                if (ValidationResult.IsValid)
                {
                    RequestGenerator RequestGenerator = new RequestGenerator();
                    RequestResult RequestResult = RequestGenerator.GenerateRequest(eInvoice);

                    if (RequestResult.IsValid)
                    {
                        Console.WriteLine(RequestResult.InvoiceRequest.Serialize());
                    }

                    byte[] bytes = Encoding.UTF8.GetBytes(eInvoice.OuterXml);
                    var base64Invoice = Convert.ToBase64String(bytes);
                    Console.WriteLine($"{base64Invoice}");

                    ShowReQuestResult(RequestResult);


                }
            }

            Console.ReadLine();
        }


        public static void ShowReQuestResult(RequestResult RequestResult)
        {
            Console.WriteLine($"Overall Sign Result: {(RequestResult.IsValid ? "Valid" : "Invalid")}");
            Console.WriteLine();
            foreach (var step in RequestResult.Steps)
            {
                Console.WriteLine($"Step: {step.StepName}");
                Console.WriteLine($"  Status: {(step.IsValid ? "Valid" : "Invalid")}");

                if (step.ErrorMessages.Any())
                {
                    Console.WriteLine("  Errors:");
                    foreach (var error in step.ErrorMessages)
                    {
                        Console.WriteLine($"    - {error}");
                    }
                }

                if (step.WarningMessages.Any())
                {
                    Console.WriteLine("  Warnings:");
                    foreach (var warning in step.WarningMessages)
                    {
                        Console.WriteLine($"    - {warning}");
                    }
                }
            }
        }

        public static void ShowSignResult(SignResult signResult)
        {
            Console.WriteLine($"Overall Sign Result: {(signResult.IsValid ? "Valid" : "Invalid")}");
            Console.WriteLine();
            foreach (var step in signResult.Steps)
            {
                Console.WriteLine($"Step: {step.StepName}");
                Console.WriteLine($"  Status: {(step.IsValid ? "Valid" : "Invalid")}");

                if (step.ErrorMessages.Any())
                {
                    Console.WriteLine("  Errors:");
                    foreach (var error in step.ErrorMessages)
                    {
                        Console.WriteLine($"    - {error}");
                    }
                }

                if (step.WarningMessages.Any())
                {
                    Console.WriteLine("  Warnings:");
                    foreach (var warning in step.WarningMessages)
                    {
                        Console.WriteLine($"    - {warning}");
                    }
                }
            }
        }
        public static void ShowValidationResult(ValidationResult validationResult)
        {
            Console.WriteLine($"Overall validation Result: {(validationResult.IsValid ? "Valid" : "Invalid")}");
            Console.WriteLine();
            foreach (var step in validationResult.ValidationSteps)
            {
                Console.WriteLine($"Step: {step.ValidationStepName}");
                Console.WriteLine($"  Status: {(step.IsValid ? "Valid" : "Invalid")}");

                if (step.ErrorMessages.Any())
                {
                    Console.WriteLine("  Errors:");
                    foreach (var error in step.ErrorMessages)
                    {
                        Console.WriteLine($"    - {error}");
                    }
                }

                if (step.WarningMessages.Any())
                {
                    Console.WriteLine("  Warnings:");
                    foreach (var warning in step.WarningMessages)
                    {
                        Console.WriteLine($"    - {warning}");
                    }
                }
            }
        }
    }
}
