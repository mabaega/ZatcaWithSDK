using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using Zatca.EInvoice.SDK;
using Zatca.EInvoice.SDK.Contracts.Models;

namespace ZatcaWithSDK
{
    internal class Class1
    {
        public static async Task Test()
        {
            // Declare onboarding info
            string PrivateKey = "MHQCAQEEIO5IhAPP+FaoBYbK1qZUl/IYpsrsMAoTNm5iDgIv/oTwoAcGBSuBBAAKoUQDQgAEAAqe5IKLffragVdM4McdjoeMxHGyKWyIqOzQUUReJud5CYyRJa95y3Cs/thxVMsk++Tr4ecYEsuDGMGm3zwFcw==";
            string CCSIDBinaryToken = "TUlJQ1BUQ0NBZU9nQXdJQkFnSUdBWkkzTGNHak1Bb0dDQ3FHU000OUJBTUNNQlV4RXpBUkJnTlZCQU1NQ21WSmJuWnZhV05wYm1jd0hoY05NalF3T1RJNE1EVTBPVFV6V2hjTk1qa3dPVEkzTWpFd01EQXdXakIxTVFzd0NRWURWUVFHRXdKVFFURVdNQlFHQTFVRUN3d05VbWw1WVdSb0lFSnlZVzVqYURFbU1DUUdBMVVFQ2d3ZFRXRjRhVzExYlNCVGNHVmxaQ0JVWldOb0lGTjFjSEJzZVNCTVZFUXhKakFrQmdOVkJBTU1IVlJUVkMwNE9EWTBNekV4TkRVdE16azVPVGs1T1RrNU9UQXdNREF6TUZZd0VBWUhLb1pJemowQ0FRWUZLNEVFQUFvRFFnQUVBQXFlNUlLTGZmcmFnVmRNNE1jZGpvZU14SEd5S1d5SXFPelFVVVJlSnVkNUNZeVJKYTk1eTNDcy90aHhWTXNrKytUcjRlY1lFc3VER01HbTN6d0ZjNk9Cd1RDQnZqQU1CZ05WSFJNQkFmOEVBakFBTUlHdEJnTlZIUkVFZ2FVd2dhS2tnWjh3Z1p3eE96QTVCZ05WQkFRTU1qRXRWRk5VZkRJdFZGTlVmRE10WldReU1tWXhaRGd0WlRaaE1pMHhNVEU0TFRsaU5UZ3RaRGxoT0dZeE1XVTBORFZtTVI4d0hRWUtDWkltaVpQeUxHUUJBUXdQTXprNU9UazVPVGs1T1RBd01EQXpNUTB3Q3dZRFZRUU1EQVF4TVRBd01SRXdEd1lEVlFRYURBaFNVbEpFTWpreU9URWFNQmdHQTFVRUR3d1JVM1Z3Y0d4NUlHRmpkR2wyYVhScFpYTXdDZ1lJS29aSXpqMEVBd0lEU0FBd1JRSWdEV3JKeTNXMThYQ0hDUEtGekwzK3J0dVc4SmNnd1BkNVpqWDNRQUtnbDdjQ0lRRGEvRkNoV0lXemFJU0pmOWx6SEs2T1hRUHhxMlVuSlRxMEVYMmttLyt5VXc9PQ==";
            string CCSIDSecret = "DrccJxm1ZIl+hRXQ6Fb4UbbtbwK91KQBSK9dy2U3E0I=";

            string x509CertificateContent = Encoding.UTF8.GetString(Convert.FromBase64String(CCSIDBinaryToken));

            string _ComplianceCheckInvoiceUrl = "https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal/compliance/invoices";

            //Load xml Document from file

            string _xmlInvoicePath = @"C:\Tmp\XmlTest\SimplifiedTest.xml";
            XmlDocument xmlInvoice = new() { PreserveWhitespace = true };
            xmlInvoice.Load(_xmlInvoicePath);

            // Signing Document
            SignResult signedInvoiceResult = new EInvoiceSigner().SignDocument(xmlInvoice, x509CertificateContent, PrivateKey);

            if (signedInvoiceResult.IsValid)
            {
                // Generate Request Api
                RequestResult requestResult = new RequestGenerator().GenerateRequest(signedInvoiceResult.SignedEInvoice);
                if (requestResult.IsValid)
                {
                    // Send Request to Compliance Check Api
                    using HttpClient _httpClient = new()
                    {
                        DefaultRequestHeaders =
                        {
                            Accept = { new MediaTypeWithQualityHeaderValue("application/json") },
                            AcceptLanguage = { new StringWithQualityHeaderValue("en") },
                            Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{CCSIDBinaryToken}:{CCSIDSecret}")))
                        }
                    };

                    // Add custom header
                    _httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2");

                    var content = new StringContent(JsonConvert.SerializeObject(requestResult.InvoiceRequest), Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(_ComplianceCheckInvoiceUrl, content);

                    var resultContent = await response.Content.ReadAsStringAsync();
                    var serverResult = JsonConvert.DeserializeObject<Dictionary<string, object>>(resultContent);

                    // Remove null values
                    var nonNullServerResult = serverResult
                        .Where(kvp => kvp.Value != null)
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    // Serialize with settings to format JSON
                    var settings = new JsonSerializerSettings
                    {
                        Formatting = Newtonsoft.Json.Formatting.Indented
                    };
                    string formattedJson = JsonConvert.SerializeObject(nonNullServerResult, settings);

                    Console.WriteLine($"\nStatusCode = {(int)response.StatusCode}-{response.StatusCode}");
                    Console.WriteLine($"\nInvoiceHash = {requestResult.InvoiceRequest.InvoiceHash}");
                    Console.WriteLine($"\nServer Result = \n {formattedJson}");
                }
            }
        }
    }
}
