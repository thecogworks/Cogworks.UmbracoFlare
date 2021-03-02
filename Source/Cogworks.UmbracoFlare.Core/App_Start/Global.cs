using System;
using Umbraco.Web;

namespace Cogworks.UmbracoFlare.Core
{
    public class Global : UmbracoApplication
    {
        protected override void OnApplicationStarting(object sender, EventArgs e)
        {
            IoCBootstrapper.IoCSetup();
            base.OnApplicationStarting(sender, e);
        }
    }
}
