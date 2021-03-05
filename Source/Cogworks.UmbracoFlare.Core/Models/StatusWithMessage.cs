namespace Cogworks.UmbracoFlare.Core.Models
{
    public class StatusWithMessage
    {
        public StatusWithMessage(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public StatusWithMessage()
        {
        }

        public bool Success { get; set; }
        public string Message { get; set; }
    }
}