using System;
using Umbraco.Core.Logging;

namespace Cogworks.UmbracoFlare.Core.Services
{
    public interface IUmbracoLoggingService
    {
        void LogError<T>(string message, Exception ex);

        void LogInfo<T>(string message);

        void LogDebug<T>(string message);

        void LogWarn<T>(string message);
    }

    public class UmbracoLoggingService : IUmbracoLoggingService
    {
        private readonly ILogger _logger;

        public UmbracoLoggingService(ILogger logger)
        {
            _logger = logger;
        }

        public void LogError<T>(string message, Exception ex)
        {
            _logger.Error(typeof(T), ex, message);
        }

        public void LogInfo<T>(string message)
        {
            _logger.Info(typeof(T), message);
        }

        public void LogDebug<T>(string message)
        {
            _logger.Debug(typeof(T), message);
        }

        public void LogWarn<T>(string message)
        {
            _logger.Warn(typeof(T), message);
        }
    }
}