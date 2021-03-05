using Cogworks.UmbracoFlare.Core.Helpers;
using System.Collections.Generic;
using Umbraco.Core.Models;
using Umbraco.Web;

namespace Cogworks.UmbracoFlare.Core.Wrappers
{
    public interface IUmbracoHelperWrapper
    {
        IEnumerable<IPublishedContent> TypedContentAtRoot();

        IPublishedContent TypedMedia(int id);

        IPublishedContent TypedContent(int id);
    }

    public class UmbracoHelperWrapper : IUmbracoHelperWrapper
    {
        private readonly UmbracoHelper _umbracoHelper;

        public UmbracoHelperWrapper(IUmbracoContextWrapper umbracoContext)
        {
            UmbracoFlareUmbracoContextHelper.EnsureContext();
            _umbracoHelper = new UmbracoHelper(umbracoContext.Current);
        }

        public IEnumerable<IPublishedContent> TypedContentAtRoot()
        {
            return _umbracoHelper.TypedContentAtRoot();
        }

        public IPublishedContent TypedMedia(int id)
        {
            return _umbracoHelper.TypedMedia(id);
        }

        public IPublishedContent TypedContent(int id)
        {
            return _umbracoHelper.TypedContent(id);
        }
    }
}