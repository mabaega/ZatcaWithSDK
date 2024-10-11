using System.Xml;

namespace ZatcaWithSDK
{
    public class CustomUrlResolver : XmlUrlResolver
    {
        public CustomUrlResolver()
        {
            // Initialize allowedUris or any other setup if needed
            allowedUris = new HashSet<string>();
        }

        // Fields
        private readonly HashSet<string> allowedUris;

        // Methods
        public override Uri ResolveUri(Uri baseUri, string relativeUri)
        {
            throw new InvalidOperationException("[Security Issue] A security vulnerability is detected. Operation aborted");
        }
    }
}
