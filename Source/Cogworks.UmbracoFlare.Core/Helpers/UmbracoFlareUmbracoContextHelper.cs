using System.IO;
using System.Web;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Web;
using Umbraco.Web.Routing;
using Umbraco.Web.Security;

namespace Cogworks.UmbracoFlare.Core.Helpers
{
    public static class UmbracoFlareUmbracoContextHelper
    {
        public static void EnsureContext()
        {
            if (UmbracoContext.Current == null)
            {
                var httpContext = HttpContext.Current != null
                    ? HttpContext.Current
                    : new HttpContext(new HttpRequest(string.Empty, "http://tempuri.org", string.Empty), new HttpResponse(new StringWriter()));

                var httpBase = new HttpContextWrapper(httpContext);

                // create UmbracoContext
                UmbracoContext.EnsureContext(
                    httpBase,
                    ApplicationContext.Current,
                    new WebSecurity(httpBase, ApplicationContext.Current),
                    UmbracoConfig.For.UmbracoSettings(),
                    UrlProviderResolver.Current.Providers,
                    false);
            }
        }
    }
}