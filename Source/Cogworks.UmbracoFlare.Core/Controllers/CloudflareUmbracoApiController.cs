using Cogworks.UmbracoFlare.Core.Constants;
using Cogworks.UmbracoFlare.Core.Extensions;
using Cogworks.UmbracoFlare.Core.Factories;
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

        public CloudflareUmbracoApiController()
        {
            _cloudflareService = ServiceFactory.GetCloudflareService();
            _umbracoFlareDomainService = ServiceFactory.GetUmbracoFlareDomainService();
            _configurationService = ServiceFactory.GetConfigurationService();
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
        public StatusWithMessage PurgeAll()
        {
            var domains = _umbracoFlareDomainService.GetAllowedCloudflareDomains();
            var results = domains
                .Where(x=> x.HasValue())
                .Select(domain => _cloudflareService.PurgeEverything(domain))
                .ToList();

            if (results.Any(x => !x.Success))
            {
                return new StatusWithMessage(false, _cloudflareService.PrintResultsSummary(results));
            }

            return new StatusWithMessage(true, $"{results.Count(x => x.Success)} domains purged successfully.");
        }

        [HttpGet]
        public IEnumerable<string> GetAllowedDomains()
        {
            return _umbracoFlareDomainService.GetAllowedCloudflareDomains();
        }

        [HttpPost]
        public StatusWithMessage PurgeStaticFiles([FromBody] PurgeStaticFilesRequestModel model)
        {
            if (!model.StaticFiles.HasAny())
            {
                return new StatusWithMessage(false, "There were not static files selected to purge");
            }

            var results = new List<StatusWithMessage>();
            var fullUrlsToPurge = new List<string>();
            var allFilePaths = _cloudflareService.GetFilePaths(model.StaticFiles);

            foreach (var filePath in allFilePaths)
            {
                var extension = Path.GetExtension(filePath);

                if (ApplicationConstants.AllowedFileExtensions.Contains(extension))
                {
                    var urls = UmbracoFlareUrlHelper.GetFullUrlForPurgeStaticFiles(filePath, model.SelectedDomains, true);
                    fullUrlsToPurge.AddRange(urls);
                }
            }

            var pageStatusMessages = _cloudflareService.PurgePages(fullUrlsToPurge);
            results.AddRange(pageStatusMessages);

            if (results.Any(x => !x.Success))
            {
                var resultsSummary = _cloudflareService.PrintResultsSummary(results);
                return new StatusWithMessage(false, resultsSummary);
            }

            return new StatusWithMessage(true, $"{results.Count(x => x.Success)} static files purged successfully.");
        }

        [HttpPost]
        public StatusWithMessage PurgeCacheForUrls([FromBody] PurgeUrlsRequestModel model)
        {
            if (!model.Urls.HasAny())
            {
                return new StatusWithMessage(false, "You must provide urls to clear the cache for.");
            }

            var builtUrls = new List<string>();

            if (model.Domains.HasAny())
            {
                builtUrls.AddRange(UmbracoFlareUrlHelper.MakeFullUrlsWithDomain(model.Urls, model.Domains, true));
            }

            var urlsWithWildCards = builtUrls.Where(x => x.Contains('*'));
            var willCardsUrls = !urlsWithWildCards.HasAny()
                ? builtUrls
                : _umbracoFlareDomainService.GetAllUrlsForWildCardUrls(urlsWithWildCards);

            builtUrls.AddRangeUnique(willCardsUrls);

            var results = _cloudflareService.PurgePages(builtUrls);

            if (results.Any(x => !x.Success))
            {
                return new StatusWithMessage(false, _cloudflareService.PrintResultsSummary(results));
            }

            return new StatusWithMessage(true, $"{results.Count(x => x.Success)} urls purged successfully.");
        }

        [HttpPost]
        public StatusWithMessage PurgeCacheForContentNode([FromBody] PurgeFromContentTree model)
        {
            if (model.NodeId <= 0)
            {
                return new StatusWithMessage(false, "You must provide a node id.");
            }

            var urls = new List<string>();
            urls.AddRange(_umbracoFlareDomainService.GetUrlsForNode(model.NodeId, model.PurgeChildren));

            var results = _cloudflareService.PurgePages(urls);

            if (results.Any(x => !x.Success))
            {
                return new StatusWithMessage(false, _cloudflareService.PrintResultsSummary(results));
            }

            return new StatusWithMessage(true, $"{results.Count(x => x.Success)} urls purged successfully.");
        }
    }
}