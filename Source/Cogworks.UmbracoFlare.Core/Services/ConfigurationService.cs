using Cogworks.UmbracoFlare.Core.Constants;
using Cogworks.UmbracoFlare.Core.Extensions;
using Cogworks.UmbracoFlare.Core.Models;
using System;
using System.IO;
using System.Web;
using System.Xml.Serialization;
using Cogworks.UmbracoFlare.Core.Factories;

namespace Cogworks.UmbracoFlare.Core.Services
{
    public interface IConfigurationService
    {
        bool ConfigurationFileHasData(UmbracoFlareConfigModel configurationFile);

        UmbracoFlareConfigModel LoadConfigurationFile();

        UmbracoFlareConfigModel SaveConfigurationFile(UmbracoFlareConfigModel configurationObject);
    }

    public class ConfigurationService : IConfigurationService
    {
        private readonly IUmbracoLoggingService _umbracoLoggingService;

        public ConfigurationService()
        {
            _umbracoLoggingService = ServiceFactory.GetUmbracoLoggingService();
        }

        public bool ConfigurationFileHasData(UmbracoFlareConfigModel configurationFile)
        {
            return configurationFile.AccountEmail.HasValue() && configurationFile.ApiKey.HasValue();
        }

        public UmbracoFlareConfigModel LoadConfigurationFile()
        {
            try
            {
                var configurationFilePath = HttpContext.Current.Server.MapPath(ApplicationConstants.ConfigurationFile.ConfigurationFilePath);
                var serializer = new XmlSerializer(typeof(UmbracoFlareConfigModel));

                using (var reader = new StreamReader(configurationFilePath))
                {
                    return (UmbracoFlareConfigModel)serializer.Deserialize(reader);
                }
            }
            catch (Exception e)
            {
                _umbracoLoggingService.LogError<IConfigurationService>($"Could not load the file in this path {ApplicationConstants.ConfigurationFile.ConfigurationFilePath}", e);
            }

            return new UmbracoFlareConfigModel();
        }

        public UmbracoFlareConfigModel SaveConfigurationFile(UmbracoFlareConfigModel configurationFile)
        {
            try
            {
                var configurationFilePath = HttpContext.Current.Server.MapPath(ApplicationConstants.ConfigurationFile.ConfigurationFilePath);
                var serializer = new XmlSerializer(typeof(UmbracoFlareConfigModel));

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