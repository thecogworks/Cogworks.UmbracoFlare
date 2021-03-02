using System;

namespace Cogworks.UmbracoFlare.Core.Services
{
    public interface IUmbracoLoggingService
    {
        void LogError<T>(string message, Exception ex);

        void LogError(Type callingType, string message, Exception ex);

        void LogInfo<T>(string message);

        void LogDebug<T>(string message);

        void LogWarn<T>(string message);
    }

    public class UmbracoLoggingService : IUmbracoLoggingService
    {
        public void LogError<T>(string message, Exception ex)
        {
            Umbraco.Core.Logging.LogHelper.Error<T>(message, ex);
        }

        public void LogError(Type callingType, string message, Exception ex)
        {
            Umbraco.Core.Logging.LogHelper.Error(callingType, message, ex);
        }
        
        public void LogInfo<T>(string message)
        {
            Umbraco.Core.Logging.LogHelper.Info<T>(message);
        }

        public void LogDebug<T>(string message)
        {
            Umbraco.Core.Logging.LogHelper.Debug<T>(message);
        }

        public void LogWarn<T>(string message)
        {
            Umbraco.Core.Logging.LogHelper.Warn<T>(message);
        }
    }
}