using System;
using InEngine.Core.Logging;
using NLog;


namespace InEngine
{
    public class NLogAdapter : ILog
    {
        public ILogger Logger { get; set; }

        public NLogAdapter() : this(null)
        {}

        public static ILog Make(string loggerName = "InEngine")
        {
            return new NLogAdapter(LogManager.GetLogger(loggerName));
        }

        public NLogAdapter(ILogger logger)
        {
            Logger = logger;
        }

        public void Debug(object message)
        {
            Logger.Debug(message);
        }

        public void Debug(Exception exception, object message)
        {
            Logger.Debug(exception, message.ToString());
        }

        public void Error(object message)
        {
            Logger.Error(message);
        }

        public void Error(Exception exception, object message)
        {
            Logger.Error(exception, message.ToString());
        }

        public void Warning(object message)
        {
            Logger.Warn(message);
        }

        public void Warning(Exception exception, object message)
        {
            Logger.Warn(exception, message.ToString());
        }
    }
}
