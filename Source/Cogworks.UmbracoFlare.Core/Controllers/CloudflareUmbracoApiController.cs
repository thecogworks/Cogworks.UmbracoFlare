using Cogworks.UmbracoFlare.Core.Constants;
using Cogworks.UmbracoFlare.Core.Extensions;
using Cogworks.UmbracoFlare.Core.Helpers;
using Cogworks.UmbracoFlare.Core.Models;
using Cogworks.UmbracoFlare.Core.Models.Api;
using Cogworks.UmbracoFlare.Core.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Http;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;

namespace Cogworks.UmbracoFlare.Core.Controllers
{
    [PluginController("UmbracoFlare")]
    public class CloudflareUmbracoApiController : UmbracoAuthorizedApiController
    {
        private readonly ICloudflareService _cloudflareService;
        private readonly IUmbracoFlareDomainService _umbracoFlareDomainService;
        private readonly IConfigurationService _configurationService;

        public CloudflareUmbracoApiController(ICloudflareService cloudflareService, IConfigurationService configurationService, IUmbracoFlareDomainService umbracoFlareDomainService)
        {
            _cloudflareService = cloudflareService;
            _configurationService = configurationService;
            _umbracoFlareDomainService = umbracoFlareDomainService;
        }

        [HttpGet]
        public UmbracoFlareConfigModel GetConfig()
        {
            var configurationFile = _configurationService.LoadConfigurationFile();
            if (!_configurationService.ConfigurationFileHasData(configurationFile))
            {
                return configurationFile;
            }

            var userDetails = _cloudflareService.GetCloudflareUserDetails();
            configurationFile.CredentialsAreValid = userDetails != null && userDetails.Success;
            configurationFile.AllowedDomains = _umbracoFlareDomainService.GetAllowedCloudflareDomains();

            return configurationFile;
        }

        [HttpPost]
        public UmbracoFlareConfigModel UpdateConfigStatus([FromBody] UmbracoFlareConfigModel config)
        {
            var configurationFile = _configurationService.SaveConfigurationFile(config);
            var userDetails = _cloudflareService.GetCloudflareUserDetails();
            configurationFile.CredentialsAreValid = userDetails != null && userDetails.Success;

            configurationFile = _configurationService.SaveConfigurationFile(config);

            return configurationFile;
        }

        [HttpPost]
        public StatusWithMessage PurgeAll([FromUri] string currentDomain)
        {
            var currentDomainIsValid = _umbracoFlareDomainService.GetAllowedCloudflareDomains().Count(x => x.Equals(currentDomain)) > 0;

            if (!currentDomainIsValid)
            {
                return new StatusWithMessage(false, "The current domain is not valid, please check if the domain is a valid zone in your cloudflare account.");
            }

            var result = _cloudflareService.PurgeEverything(currentDomain);
            return result;
        }

        [HttpPost]
        public StatusWithMessage PurgeStaticFiles([FromBody] PurgeStaticFilesRequestModel model)
        {
            if (!model.StaticFiles.HasAny())
            {
                return new StatusWithMessage(false, "There were not static files selected to purge");
            }

            var currentDomainIsValid = _umbracoFlareDomainService.GetAllowedCloudflareDomains().Count(x => x.Equals(model.CurrentDomain)) > 0;

            if (!currentDomainIsValid)
            {
                return new StatusWithMessage(false, "The current domain is not valid, please check if the domain is a valid zone in your cloudflare account.");
            }
            
            var fullUrlsToPurge = new List<string>();
            var allFilePaths = _cloudflareService.GetFilePaths(model.StaticFiles);

            foreach (var filePath in allFilePaths)
            {
                var extension = Path.GetExtension(filePath);

                if (ApplicationConstants.AllowedFileExtensions.Contains(extension))
                {
                    var urls = UmbracoFlareUrlHelper.GetFullUrlForPurgeStaticFiles(filePath, model.CurrentDomain, true);
                    fullUrlsToPurge.AddRange(urls);
                }
            }

            var result = _cloudflareService.PurgePages(fullUrlsToPurge);
            
            return !result.Success ? result : new StatusWithMessage(true, $"{fullUrlsToPurge.Count()} static files were purged successfully.");
        }

        [HttpPost]
        public StatusWithMessage PurgeCacheForUrls([FromBody] PurgeUrlsRequestModel model)
        {
            if (!model.Urls.HasAny())
            {
                return new StatusWithMessage(false, "You must provide urls to clear the cache for.");
            }

            var currentDomainIsValid = _umbracoFlareDomainService.GetAllowedCloudflareDomains().Count(x => x.Equals(model.CurrentDomain)) > 0;

            if (!currentDomainIsValid)
            {
                return new StatusWithMessage(false, "The current domain is not valid, please check if the domain is a valid zone in your cloudflare account.");
            }
            
            var builtUrls = new List<string>();
            builtUrls.AddRange(UmbracoFlareUrlHelper.MakeFullUrlsWithDomain(model.Urls, model.CurrentDomain, true));
            
            var urlsWithWildCards = builtUrls.Where(x => x.Contains('*'));
            var willCardsUrls = !urlsWithWildCards.HasAny()
                ? builtUrls
                : _umbracoFlareDomainService.GetAllUrlsForWildCardUrls(urlsWithWildCards);

            builtUrls.AddRangeUnique(willCardsUrls);

            var result = _cloudflareService.PurgePages(builtUrls);

            return !result.Success ? result : new StatusWithMessage(true, $"{builtUrls.Count()} urls purged successfully.");
        }

        [HttpPost]
        public StatusWithMessage PurgeCacheForContentNode([FromBody] PurgeFromContentTree model)
        {
            if (model.NodeId <= 0)
            {
                return new StatusWithMessage(false, "You must provide a node id.");
            }

            var currentDomainIsValid = _umbracoFlareDomainService.GetAllowedCloudflareDomains().Count(x => x.Equals(model.CurrentDomain)) > 0;

            if (!currentDomainIsValid)
            {
                return new StatusWithMessage(false, "The current domain is not valid, please check if the domain is a valid zone in your cloudflare account.");
            }

            var urls = new List<string>();
            urls.AddRange(_umbracoFlareDomainService.GetUrlsForNode(model.NodeId, model.CurrentDomain, model.PurgeChildren));

            var result = _cloudflareService.PurgePages(urls);

            return !result.Success ? result : new StatusWithMessage(true, $"{urls.Count()} urls purged successfully.");
        }
    }
}