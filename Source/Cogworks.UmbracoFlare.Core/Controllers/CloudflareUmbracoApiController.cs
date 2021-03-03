using Cogworks.UmbracoFlare.Core.Extensions;
using Cogworks.UmbracoFlare.Core.FileSystemPickerControllers;
using Cogworks.UmbracoFlare.Core.Helpers;
using Cogworks.UmbracoFlare.Core.Models;
using Cogworks.UmbracoFlare.Core.Models.Api;
using Cogworks.UmbracoFlare.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Http;
using Umbraco.Core.IO;
using Umbraco.Core.Models;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;
using File = System.IO.File;

namespace Cogworks.UmbracoFlare.Core.Controllers
{
    [PluginController("UmbracoFlare")]
    public class CloudflareUmbracoApiController : UmbracoAuthorizedApiController
    {
        private readonly ICloudflareService _cloudflareService;
        private readonly IUmbracoFlareDomainService _umbracoFlareDomainService;
        private readonly IUmbracoLoggingService _umbracoLoggingService;
        private readonly IConfigurationService _configurationService;

        public CloudflareUmbracoApiController(ICloudflareService cloudflareService, IUmbracoFlareDomainService umbracoFlareDomainService,
            IUmbracoLoggingService umbracoLoggingService, IConfigurationService configurationService)
        {
            _cloudflareService = cloudflareService;
            _umbracoFlareDomainService = umbracoFlareDomainService;
            _umbracoLoggingService = umbracoLoggingService;
            _configurationService = configurationService;
        }

        [HttpGet]
        public CloudflareConfigModel GetConfig()
        {
            var configurationFile = _configurationService.LoadConfigurationFile();
            if (!_configurationService.ConfigurationFileHasData(configurationFile))
            {
                return configurationFile;
            }

            var userDetails = _cloudflareService.GetCloudflareUserDetails(configurationFile);
            configurationFile.CredentialsAreValid = userDetails != null && userDetails.Success;

            return configurationFile;
        }

        [HttpPost]
        public CloudflareConfigModel UpdateConfigStatus([FromBody] CloudflareConfigModel config)
        {
            var userDetails = _cloudflareService.GetCloudflareUserDetails(config);
            config.CredentialsAreValid = userDetails != null && userDetails.Success;

            var configurationFile = _configurationService.SaveConfigurationFile(config);

            return configurationFile;
        }

        [HttpPost]
        public StatusWithMessage PurgeAll()
        {
            var domains = _umbracoFlareDomainService.GetAllowedCloudflareDomains();
            var results = domains.Select(domain => _cloudflareService.PurgeEverything(domain)).ToList();

            return new StatusWithMessage { Success = results.All(x => x.Success), Message = _cloudflareService.PrintResultsSummary(results) };
        }

        [HttpGet]
        public IEnumerable<string> GetAllowedDomains()
        {
            return _umbracoFlareDomainService.GetAllowedCloudflareDomains();
        }

        [HttpPost]
        public StatusWithMessage PurgeCacheForUrls([FromBody] PurgeCacheForUrlsRequestModel model)
        {
            /*Important to note that the urls can come in here in two different ways.
             *1) They can come in here without domains on them. If that is the case then the domains property should have values.
             *      1a) They will need to have the urls built by appending each domain to each url. These urls technically might not exist
             *          but that is the responsibility of whoever called this method to ensure that. They will still go to cloudflare even know the
             *          urls physically do not exists, which is fine because it won't cause an error.
             *2) They can come in here with domains, if that is the case then we are good to go, no work needed.
             *
             * */

            if (model.Urls == null || !model.Urls.Any())
            {
                return new StatusWithMessage(false, "You must provide urls to clear the cache for.");
            }

            var builtUrls = new List<string>();

            if (model.Domains.HasAny())
            {
                builtUrls.AddRange(UrlHelper.MakeFullUrlWithDomain(model.Urls, model.Domains, true));
            }
            else
            {
                builtUrls = model.Urls.ToList();
            }

            builtUrls.AddRange(AccountForWildCards(builtUrls));

            var results = _cloudflareService.PurgePages(builtUrls);

            if (results.Any(x => !x.Success))
            {
                return new StatusWithMessage(false, _cloudflareService.PrintResultsSummary(results));
            }

            return new StatusWithMessage(true, $"{results.Count(x => x.Success)} urls purged successfully.");
        }

        [HttpPost]
        public StatusWithMessage PurgeStaticFiles([FromBody] PurgeStaticFilesRequestModel model)
        {
            var allowedFileExtensions = new List<string> { ".css", ".js", ".jpg", ".png", ".gif", ".aspx", ".html" };
            const string generalSuccessMessage = "Successfully purged the cache for the selected static files.";
            const string generalErrorMessage = "Sorry, we could not purge the cache for the static files.";

            if (model.StaticFiles == null)
            {
                return new StatusWithMessage(false, generalErrorMessage);
            }

            if (!model.StaticFiles.Any())
            {
                return new StatusWithMessage(true, generalSuccessMessage);
            }

            var allFilePaths = GetAllFilePaths(model.StaticFiles, out var errors);
            var results = new List<StatusWithMessage>();
            var fullUrlsToPurge = new List<string>();

            foreach (var filePath in allFilePaths)
            {
                var extension = Path.GetExtension(filePath);

                if (allowedFileExtensions.Contains(extension))
                {
                    fullUrlsToPurge.AddRange(UrlHelper.MakeFullUrlWithDomain(filePath, model.Hosts, true));
                }
            }

            results.AddRange(_cloudflareService.PurgePages(fullUrlsToPurge));

            if (results.Any(x => !x.Success))
            {
                return new StatusWithMessage(false, _cloudflareService.PrintResultsSummary(results));
            }

            return new StatusWithMessage(true, $"{results.Count(x => x.Success)} static files purged successfully.");
        }

        private IEnumerable<string> GetAllFilePaths(IEnumerable<string> filesOrFolders, out List<StatusWithMessage> errors)
        {
            errors = new List<StatusWithMessage>();

            var rootOfApplication = IOHelper.MapPath("~/");
            var filePaths = new List<string>();
            var fileSystemApi = new FileSystemPickerApiController();
            var filesOrFoldersTest = filesOrFolders.HasAny() ? filesOrFolders.Where(x => x.HasValue()) : Enumerable.Empty<string>();

            foreach (var fileOrFolder in filesOrFoldersTest)
            {
                try
                {
                    var fileOrFolderPath = IOHelper.MapPath(fileOrFolder);
                    var fileAttributes = File.GetAttributes(fileOrFolderPath);

                    if (fileAttributes.Equals(FileAttributes.Directory))
                    {
                        var filesInTheFolder = fileSystemApi.GetFilesIncludingSubDirs(fileOrFolderPath);

                        filePaths.AddRange(filesInTheFolder.Where(x => x.HasValue()).Select(x =>
                         {
                             var directory = x.Directory.FullName.Replace(rootOfApplication, "");
                             directory = directory.Replace("\\", "/");

                             return directory + "/" + x.Name;
                         }));
                    }
                    else
                    {
                        if (!File.Exists(fileOrFolderPath))
                        {
                            _umbracoLoggingService.LogWarn<CloudflareUmbracoApiController>($"Could not find file with the path {fileOrFolderPath}");
                            continue;
                        }

                        filePaths.Add(fileOrFolder.StartsWith("/") ? fileOrFolder.TrimStart('/') : fileOrFolder);
                    }
                }
                catch (Exception)
                {
                }
            }

            return filePaths;
        }

        [HttpPost]
        public StatusWithMessage PurgeCacheForContentNode([FromBody] PurgeCacheForIdParams args)
        {
            if (args.NodeId <= 0)
            {
                return new StatusWithMessage(false, "You must provide a node id.");
            }

            var content = Umbraco.TypedContent(args.NodeId);
            var urls = BuildUrlsToPurge(content, args.PurgeChildren);
            var resultFromPurge = PurgeCacheForUrls(new PurgeCacheForUrlsRequestModel { Urls = urls, Domains = null });

            return resultFromPurge.Success ? new StatusWithMessage(true, resultFromPurge.Message) : resultFromPurge;
        }

        private IEnumerable<string> BuildUrlsToPurge(IPublishedContent contentToPurge, bool includeChildren)
        {
            var urls = new List<string>();

            if (contentToPurge == null) { return urls; }

            urls.AddRange(_umbracoFlareDomainService.GetUrlsForNode(contentToPurge.Id, includeChildren));
            return urls;
        }

        private IEnumerable<string> AccountForWildCards(IEnumerable<string> urls)
        {
            var urlsWithWildCards = urls.Where(x => x.Contains('*'));
            return !urlsWithWildCards.HasAny() ? urls : _umbracoFlareDomainService.GetAllUrlsForWildCardUrls(urlsWithWildCards);
        }
    }
}