using Cogworks.UmbracoFlare.Core.Helpers;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Web;

namespace Cogworks.UmbracoFlare.Core.Wrappers
{
    public interface IUmbracoContextWrapper : IDisposeOnRequestEnd
    {
        UmbracoContext Current { get; }

        ApplicationContext ApplicationContext { get; }

        int? PageId { get; }

        IPublishedContent CurrentPage { get; }
    }

    public class UmbracoContextWrapper : IUmbracoContextWrapper
    {
        public UmbracoContextWrapper()
        {
            UmbracoFlareUmbracoContextHelper.EnsureContext();
        }

        public UmbracoContext Current => UmbracoContext.Current;

        public ApplicationContext ApplicationContext => ApplicationContext.Current;

        public virtual int? PageId => UmbracoContext.Current.PageId;

        public virtual IPublishedContent CurrentPage => UmbracoContext.Current.PublishedContentRequest.PublishedContent;

        public void Dispose()
        {
            if (UmbracoContext.Current != null)
            {
                UmbracoContext.Current.Dispose();
            }
        }
    }
}