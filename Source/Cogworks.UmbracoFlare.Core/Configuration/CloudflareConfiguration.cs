using Cogworks.UmbracoFlare.Core.Services;
using System;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;

namespace Cogworks.UmbracoFlare.Core.Configuration
{
    //maybe use this to get and save the whole file https://stackoverflow.com/questions/14410370/reading-and-writing-to-a-custom-config-in-c-sharp
    public class CloudflareConfiguration
    {
        public static string ConfigPath;
        private readonly XDocument _doc;
        private static CloudflareConfiguration _instance;
        public static CloudflareConfiguration Instance => _instance ?? (_instance = new CloudflareConfiguration(DependencyResolver.Current.GetService<IUmbracoLoggingService>()));

        public string ApiKey
        {
            get => _doc == null ? string.Empty : _doc.Root.Element("apiKey").Value;
            set
            {
                _doc.Root.Element("apiKey").SetValue(value);
                _doc.Save(ConfigPath);
            }
        }

        public string AccountEmail
        {
            get => _doc == null ? string.Empty : _doc.Root.Element("accountEmail").Value;
            set
            {
                _doc.Root.Element("accountEmail").SetValue(value);
                _doc.Save(ConfigPath);
            }
        }

        public bool PurgeCacheOn
        {
            get => _doc?.Root.Element("purgeCacheOn").Value.Equals("true") ?? false;

            set
            {
                _doc.Root.Element("purgeCacheOn").SetValue(value.ToString());
                _doc.Save(ConfigPath);
            }
        }

        private CloudflareConfiguration(IUmbracoLoggingService umbracoLoggingService)
        {
            try
            {
                ConfigPath = HttpContext.Current.Server.MapPath("~/Config/cloudflare.config");
                _doc = XDocument.Load(ConfigPath);
            }
            catch (Exception e)
            {
                umbracoLoggingService.LogError<CloudflareConfiguration>($"Could not load the file in this path {ConfigPath}", e);
            }
        }
    }
}