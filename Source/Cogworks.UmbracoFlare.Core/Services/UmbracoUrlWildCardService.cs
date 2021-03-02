using Cogworks.UmbracoFlare.Core.Constants;
using Cogworks.UmbracoFlare.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Cogworks.UmbracoFlare.Core.Wrappers;
using Umbraco.Web;

namespace Cogworks.UmbracoFlare.Core.Services
{
    public interface IUmbracoUrlWildCardService
    {
        IEnumerable<string> GetAllUrlsForWildCardUrls(IEnumerable<string> wildCardUrls);

        void UpdateContentIdToUrlCache(int id, IEnumerable<string> urls);

        void DeletedContentIdToUrlCache();
    }

    public class UmbracoUrlWildCardService : IUmbracoUrlWildCardService
    {
        private readonly IUmbracoFlareDomainService _umbracoFlareDomainService;
        private readonly IUmbracoHelperWrapper _umbracoHelperWrapper;
        private Dictionary<int, IEnumerable<string>> _contentIdToUrlCache;

        public UmbracoUrlWildCardService(IUmbracoHelperWrapper umbracoHelperWrapper, IUmbracoFlareDomainService umbracoFlareDomainService)
        {
            _umbracoHelperWrapper = umbracoHelperWrapper;
            _umbracoFlareDomainService = umbracoFlareDomainService;
        }

        public IEnumerable<string> GetAllUrlsForWildCardUrls(IEnumerable<string> wildCardUrls)
        {
            var resolvedUrls = new List<string>();
            if (!wildCardUrls.HasAny()) { return resolvedUrls; }

            var allContentUrls = GetAllContentUrls();

            foreach (var wildCardUrl in wildCardUrls)
            {
                if (!wildCardUrl.Contains('*')) { continue; }

                //Make one for modifying
                var mutableWildCardUrl = wildCardUrl;

                mutableWildCardUrl = mutableWildCardUrl.TrimEnd('/');
                mutableWildCardUrl = mutableWildCardUrl.TrimEnd('*');

                //We can get wild cards by seeing if any of the urls start with the mutable wild card url
                resolvedUrls.AddRange(allContentUrls.Where(x => x.StartsWith(mutableWildCardUrl)));
            }

            return resolvedUrls;
        }

        public void UpdateContentIdToUrlCache(int id, IEnumerable<string> urls)
        {
            if (!_contentIdToUrlCache.HasAny())
            {
                return;
            }

            if (_contentIdToUrlCache.ContainsKey(id))
            {
                _contentIdToUrlCache[id] = urls;
            }
            else
            {
                _contentIdToUrlCache.Add(id, urls);
            }
        }

        public void DeletedContentIdToUrlCache()
        {
            HttpRuntime.Cache.Remove(ApplicationConstants.CacheKeys.UmbracoUrlWildCardServiceCacheKey);
            _contentIdToUrlCache = null;
        }

        //CACHE THIS METHOD BETTER
        private IEnumerable<string> GetAllContentUrls()
        {
            if (_contentIdToUrlCache != null && _contentIdToUrlCache.Any())
            {
                //just return the cache
                return this._contentIdToUrlCache.SelectMany(x => x.Value);
            }

            var cache = new Dictionary<int, IEnumerable<string>>();
            var urls = new List<string>();

            //Id like to use UmbracoContext.Current.ContentCache.GetByRoute() somehow but you cant always guarantee that urls
            //will be in  hierarchical order because of rewriting, etc.
            var roots = _umbracoHelperWrapper.TypedContentAtRoot();

            foreach (var content in roots)
            {
                var contentUrls = _umbracoFlareDomainService.GetUrlsForNode(content.Id);

                cache.Add(content.Id, contentUrls);
                urls.AddRange(contentUrls);

                foreach (var childContent in content.Descendants())
                {
                    var childContentUrls = _umbracoFlareDomainService.GetUrlsForNode(childContent.Id);

                    cache.Add(childContent.Id, childContentUrls);
                    urls.AddRange(childContentUrls);
                }
            }

            _contentIdToUrlCache = cache;
            //Add to the cache
            HttpRuntime.Cache.Insert(ApplicationConstants.CacheKeys.UmbracoUrlWildCardServiceCacheKey,
                cache, null, DateTime.Now.AddDays(1),
                System.Web.Caching.Cache.NoSlidingExpiration,
                System.Web.Caching.CacheItemPriority.Normal,
                null);

            return urls;
        }
    }
}