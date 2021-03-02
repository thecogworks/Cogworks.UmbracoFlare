using System;

namespace Cogworks.UmbracoFlare.Core.Constants
{
    public static class ApplicationConstants
    {
        public static class UmbracoFlareBackendProperties
        {
            public const string CloudflareDisabledOnPublishPropertyAlias = "cloudflareDisabledOnPublish";
        }

        public static class EventMessageCategory
        {
            public const string CloudflareCaching = "Cloudflare Caching";
        }

        public static class CloudflareMessages
        {
            public const string CloudflareDisabled = "We could not purge the cache because your settings indicate that cloudflare purging is off.";
            public const string CloudflareApiError = "There was an error from the Cloudflare API. Please check the logfile to see the issue.";
        }

        public static class CacheKeys
        {
            public const string UmbracoUrlWildCardServiceCacheKey = "UmbracoUrlWildCardService.ContentIdToUrlCache";
        }

        public static Guid ContentIdToUrlCacheRefresherGuid = Guid.NewGuid();
    }
}