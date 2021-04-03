using Cogworks.UmbracoFlare.Core.Client;
using Cogworks.UmbracoFlare.Core.Controllers;
using Cogworks.UmbracoFlare.Core.Extensions;
using Cogworks.UmbracoFlare.Core.Factories;
using Cogworks.UmbracoFlare.Core.Helpers;
using Cogworks.UmbracoFlare.Core.Models;
using Cogworks.UmbracoFlare.Core.Models.Cloudflare;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Umbraco.Core.IO;

namespace Cogworks.UmbracoFlare.Core.Services
{
    public interface ICloudflareService
    {
        StatusWithMessage PurgePages(IEnumerable<string> urls);

        StatusWithMessage PurgeEverything(string currentDomain);

        UserDetails GetCloudflareUserDetails();

        IEnumerable<string> GetFilePaths(IEnumerable<string> filesOrFolders);
    }

    public class CloudflareService : ICloudflareService
    {
        private readonly IUmbracoLoggingService _umbracoLoggingService;
        private readonly ICloudflareApiClient _cloudflareApiClient;
        private readonly IUmbracoFlareDomainService _umbracoFlareDomainService;

        public CloudflareService()
        {
            _umbracoLoggingService = ServiceFactory.GetUmbracoLoggingService();
            _cloudflareApiClient = ServiceFactory.GetCloudflareApiClient();
            _umbracoFlareDomainService = ServiceFactory.GetUmbracoFlareDomainService();
        }

        public StatusWithMessage PurgePages(IEnumerable<string> urls)
        {
            if (!urls.HasAny())
            {
                return new StatusWithMessage(false, "There were not valid urls to purge, please check if the domain is a valid zone in your cloudflare account");
            }

            var currentDomain = UmbracoFlareUrlHelper.GetCurrentDomain();
            var websiteZone = _umbracoFlareDomainService.GetZoneFilteredByDomain(currentDomain);

            if (websiteZone == null)
            {
                return new StatusWithMessage(false, $"Could not retrieve the zone from cloudflare with the domain of {currentDomain}");
            }

            var apiResult = _cloudflareApiClient.PurgeCache(websiteZone.Id, urls, false);

            return apiResult
                ? new StatusWithMessage(true, "The values were purged successfully")
                : new StatusWithMessage(false, "There was an error from the Cloudflare API. Please check the logs to see the issue.");
        }

        public StatusWithMessage PurgeEverything(string currentDomain)
        {
            var websiteZone = _umbracoFlareDomainService.GetZoneFilteredByDomain(currentDomain);

            if (websiteZone == null)
            {
                return new StatusWithMessage(
                    false,
                    $"We could not purge the cache because the domain {currentDomain} is not valid with the provided credentials. Please ensure this domain is registered under these credentials on your cloudflare dashboard.");
            }

            var purgeCacheStatus = _cloudflareApiClient.PurgeCache(websiteZone.Id, Enumerable.Empty<string>(), true);

            return purgeCacheStatus
                ? new StatusWithMessage(true, $"Your current domain {currentDomain} was purged successfully.")
                : new StatusWithMessage(false, "There was an error from the Cloudflare API. Please check the logfile to see the issue.");
        }

        public UserDetails GetCloudflareUserDetails()
        {
            return _cloudflareApiClient.GetUserDetails();
        }

        public IEnumerable<string> GetFilePaths(IEnumerable<string> filesOrFolders)
        {
            var rootOfApplication = IOHelper.MapPath("~/");
            var filePaths = new List<string>();
            var filesOrFoldersTest = filesOrFolders.HasAny() ? filesOrFolders.Where(x => x.HasValue()) : Enumerable.Empty<string>();

            foreach (var fileOrFolder in filesOrFoldersTest)
            {
                var fileOrFolderPath = IOHelper.MapPath(fileOrFolder);
                var fileAttributes = File.GetAttributes(fileOrFolderPath);

                if (fileAttributes.Equals(FileAttributes.Directory))
                {
                    var filesInTheFolder = UmbracoFlareFileHelper.GetFilesIncludingSubDirs(fileOrFolderPath);
                    var files = filesInTheFolder.Where(x => x.HasValue());

                    foreach (var file in files)
                    {
                        if (file.Directory == null) { continue; }

                        var directory = file.Directory.FullName.Replace(rootOfApplication, string.Empty);
                        var filePath = $"{directory.Replace("\\", "/")}/{file.Name}";

                        filePaths.Add(filePath);
                    }
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

            return filePaths;
        }
    }
}