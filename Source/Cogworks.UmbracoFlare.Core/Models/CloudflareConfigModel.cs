namespace Cogworks.UmbracoFlare.Core.Models
{
    public class CloudflareConfigModel
    {
        public bool PurgeCacheOn { get; set; }
        public string ApiKey { get; set; }
        public string AccountEmail { get; set; }
        public bool CredentialsAreValid { get; set; }
    }
}