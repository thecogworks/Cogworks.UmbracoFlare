using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net;
using Umbraco.Core.Composing;

namespace Cogworks.UmbracoFlare.Core.Components
{
    public class UmbracoFlareStartupComponent : IComponent
    {
        public void Initialize()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        public void Terminate()
        {
        }
    }
}