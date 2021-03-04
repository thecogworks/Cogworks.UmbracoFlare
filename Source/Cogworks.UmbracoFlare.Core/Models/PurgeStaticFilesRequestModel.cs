using System.Collections.Generic;

namespace Cogworks.UmbracoFlare.Core.Models
{
    public class PurgeStaticFilesRequestModel
    {
        public string[] StaticFiles { get; set; }
        public IEnumerable<string> SelectedDomains { get; set; }
    }
}
