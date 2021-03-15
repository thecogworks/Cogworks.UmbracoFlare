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

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        public void Terminate()
        {
        }
    }
}