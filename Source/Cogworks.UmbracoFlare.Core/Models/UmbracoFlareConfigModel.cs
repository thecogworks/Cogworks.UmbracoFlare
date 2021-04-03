using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Cogworks.UmbracoFlare.Core.Models
{
    [XmlRoot(Namespace = "cloudflare", ElementName = "cloudflare")]
    public class UmbracoFlareConfigModel
    {
        public UmbracoFlareConfigModel()
        {
            AllowedDomains = Enumerable.Empty<string>();
        }

        [XmlElement(ElementName = "purgeCacheOn")]
        public bool PurgeCacheOn { get; set; }

        [XmlElement(ElementName = "apiKey", IsNullable = true)]
        public string ApiKey { get; set; }

        [XmlElement(ElementName = "accountEmail", IsNullable = true)]
        public string AccountEmail { get; set; }

        [XmlElement(ElementName = "credentialsAreValid")]
        public bool CredentialsAreValid { get; set; }

        [XmlIgnore]
        public IEnumerable<string> AllowedDomains { get; set; }
    }
}