using Cogworks.UmbracoFlare.Core.Client;
using Cogworks.UmbracoFlare.Core.Services;
using Umbraco.Core.Services;

namespace Cogworks.UmbracoFlare.Core.Factories
{
    public static class ServiceFactory
    {
        public static IUmbracoLoggingService GetUmbracoLoggingService()
        {
            return new UmbracoLoggingService();
        }

        public static IDomainService GetDomainService()
        {
            return Umbraco.Core.Composing.Current.Services.DomainService;
        }

        public static IDataTypeService GetDataTypeService()
        {
            return Umbraco.Core.Composing.Current.Services.DataTypeService;
        }

        public static IConfigurationService GetConfigurationService()
        {
            return new ConfigurationService();
        }
        
        public static ICloudflareApiClient GetCloudflareApiClient()
        {
            return new CloudflareApiClient();
        }

        public static IUmbracoFlareDomainService GetUmbracoFlareDomainService()
        {
            return new UmbracoFlareDomainService();
        }

        public static ICloudflareService GetCloudflareService()
        {
            return new CloudflareService();
        }

        public static IImageCropperService GetImageCropperService()
        {
            return new ImageCropperService();
        }
    }
}