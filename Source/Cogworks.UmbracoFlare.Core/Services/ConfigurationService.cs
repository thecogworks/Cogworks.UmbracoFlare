using Cogworks.UmbracoFlare.Core.Constants;
using Cogworks.UmbracoFlare.Core.Extensions;
using Cogworks.UmbracoFlare.Core.Models;
using System;
using System.IO;
using System.Web;
using System.Xml.Serialization;

namespace Cogworks.UmbracoFlare.Core.Services
{
    public interface IConfigurationService
    {
        bool ConfigurationFileHasData(CloudflareConfigModel configurationFile);

        CloudflareConfigModel LoadConfigurationFile();

        CloudflareConfigModel SaveConfigurationFile(CloudflareConfigModel configurationObject);
    }

    public class ConfigurationService : IConfigurationService
    {
        private readonly IUmbracoLoggingService _umbracoLoggingService;

        public ConfigurationService(IUmbracoLoggingService umbracoLoggingService)
        {
            _umbracoLoggingService = umbracoLoggingService;
        }

        public bool ConfigurationFileHasData(CloudflareConfigModel configurationFile)
        {
            return configurationFile.AccountEmail.HasValue() && configurationFile.ApiKey.HasValue();
        }

        public CloudflareConfigModel LoadConfigurationFile()
        {
            try
            {
                var configurationFilePath = HttpContext.Current.Server.MapPath(ApplicationConstants.ConfigurationFile.ConfigurationFilePath);
                var serializer = new XmlSerializer(typeof(CloudflareConfigModel));

                using (var reader = new StreamReader(configurationFilePath))
                {
                    return (CloudflareConfigModel)serializer.Deserialize(reader);
                }
            }
            catch (Exception e)
            {
                _umbracoLoggingService.LogError<IConfigurationService>($"Could not load the file in this path {ApplicationConstants.ConfigurationFile.ConfigurationFilePath}", e);
            }

            return new CloudflareConfigModel();
        }

        public CloudflareConfigModel SaveConfigurationFile(CloudflareConfigModel configurationFile)
        {
            try
            {
                var configurationFilePath = HttpContext.Current.Server.MapPath(ApplicationConstants.ConfigurationFile.ConfigurationFilePath);
                var serializer = new XmlSerializer(typeof(CloudflareConfigModel));

                using (var writer = new StreamWriter(configurationFilePath))
                {
                    serializer.Serialize(writer, configurationFile);
                }

                return configurationFile;
            }
            catch (Exception e)
            {
                _umbracoLoggingService.LogError<IConfigurationService>($"Could not save the configuration file in this path {ApplicationConstants.ConfigurationFile.ConfigurationFilePath}", e);
            }

            return null;
        }
    }
}