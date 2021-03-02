using Cogworks.UmbracoFlare.Core.Client;
using Cogworks.UmbracoFlare.Core.Services;
using Cogworks.UmbracoFlare.Core.Wrappers;

namespace Cogworks.UmbracoFlare.Core.Factories
{
    public static class ServiceFactory
    {
        public static IUmbracoLoggingService GetUmbracoLoggingService()
        {
            return new UmbracoLoggingService();
        }

        public static IUmbracoContextWrapper GetUmbracoContextWrapper()
        {
            return new UmbracoContextWrapper();
        }

        public static IUmbracoHelperWrapper GetUmbracoHelperWrapper()
        {
            return new UmbracoHelperWrapper(GetUmbracoContextWrapper());
        }

        public static ICloudflareApiClient GetCloudflareApiClient()
        {
            return new CloudflareApiClient(GetUmbracoLoggingService());
        }

        public static ICloudflareService GetCloudflareService()
        {
            return new CloudflareService(GetUmbracoLoggingService(), GetCloudflareApiClient());
        }

        //public static IUmbracoFlareDomainService GetUmbracoFlareDomainService()
        //{
        //    return new UmbracoFlareDomainService(GetCloudflareApiClient(), GetUmbracoHelperWrapper());
        //}
    }
}