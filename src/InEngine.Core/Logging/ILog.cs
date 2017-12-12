using System;

namespace InEngine.Core.Logging
{
    public interface ILog
    {
        void Debug(object message);
        void Debug(Exception exception, object message);

        void Warning(object message);
        void Warning(Exception exception, object message);

        void Error(object message);
        void Error(Exception exception, object message);
    }
}
