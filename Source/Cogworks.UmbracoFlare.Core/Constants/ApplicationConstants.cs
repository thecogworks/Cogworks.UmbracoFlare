namespace Cogworks.UmbracoFlare.Core.Constants
{
    public static class ApplicationConstants
    {
        public const string ContentTypeApplicationJson = "application/json";
        public static readonly string[] AllowedFileExtensions = { ".css", ".js", ".jpg", ".png", ".gif", ".aspx", ".html" };
        public const string ImageCropperPropertyEditorAlias = "Umbraco.ImageCropper";

        public static class UmbracoFlareBackendProperties
        {
            public const string CloudflareDisabledOnPublishPropertyAlias = "cloudflareDisabledOnPublish";
        }

        public static class EventMessageCategory
        {
            public const string CloudflareCaching = "Cloudflare Caching";
        }
        
        public static class ConfigurationFile
        {
            public const string ConfigurationFilePath = "~/Config/cloudflare.config";
        }
    }
}