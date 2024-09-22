using Newtonsoft.Json;
using System.Text;
using System.Xml;
using Zatca.EInvoice.SDK;
using Zatca.EInvoice.SDK.Contracts.Models;


namespace SDKValidation
{
    internal class Program
    {
        static void Main(string[] args)
        {

            var ObboardingJsonPath = @"..\..\..\Data\MyCertificate\ObboardingInfo.json";
            var CleanXmlPath = @"..\..\..\Data\InvSample\CleanSimplified_Invoice.xml";
            var SignedXmlPath = @"..\..\..\Data\InvSample\Signed_Simplified_Invoice.xml";

            OnboardingResult onboardingResult = DeserializeFromFile(ObboardingJsonPath);

            XmlDocument document = new() { PreserveWhitespace = true };
            document.Load(CleanXmlPath);

            string PCSID_Certificate = Encoding.UTF8.GetString(Convert.FromBase64String(onboardingResult.PCSIDBinaryToken));
            SignResult signResult = new EInvoiceSigner().SignDocument(document, PCSID_Certificate, onboardingResult.PrivateKey);
            if (signResult.IsValid)
            {
                Console.WriteLine("Signing Invoice : " + signResult.IsValid);
                signResult.SaveSignedEInvoice(SignedXmlPath);
                Console.WriteLine("Signed file parh : "  + SignedXmlPath);
            }
            else {
                foreach (var r in signResult.Steps)
                {
                    Console.WriteLine(r.StepName);
                    Console.WriteLine(r.IsValid);
                    
                    foreach (var w in r.ErrorMessages)
                    {
                        Console.WriteLine(w.ToString());
                    }

                    foreach (var e in r.ErrorMessages)
                    {
                        Console.WriteLine(e.ToString());
                    }

                    Console.WriteLine();
                }
            }

        }

        public static OnboardingResult DeserializeFromFile(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<OnboardingResult>(json);
        }

        public class OnboardingResult
        {
            public string GeneratedCsr { get; set; }
            public string PrivateKey { get; set; }
            public string CCSIDComplianceRequestId { get; set; }
            public string CCSIDBinaryToken { get; set; }
            public string CCSIDSecret { get; set; }
            public string PCSIDBinaryToken { get; set; }
            public string PCSIDSecret { get; set; }
        }
    }
}


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