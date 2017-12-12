using System;
using InEngine.Core.IO;
using Newtonsoft.Json;

namespace InEngine.Core.Logging
{
    public class Log : ILog
    {
        IWrite Write { get; set; } = new Write(false);

        public void Debug(object message)
        {
            Write.Line(message.ToString());
        }

        public void Debug(Exception exception, object message)
        {
            Write.Line(message.ToString());
            Write.Line(JsonConvert.SerializeObject(exception));
        }

        public void Error(object message)
        {
            Write.Error(message.ToString());
        }

        public void Error(Exception exception, object message)
        {
            Write.Error(message.ToString());
            Write.Error(JsonConvert.SerializeObject(exception));
        }

        public void Warning(object message)
        {
            Write.Warning(message.ToString());
        }

        public void Warning(Exception exception, object message)
        {
            Write.Warning(message.ToString());
            Write.Warning(JsonConvert.SerializeObject(exception));
        }
    }
}
