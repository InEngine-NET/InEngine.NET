using System;
using System.Configuration;
using InEngine.Core.Queue;

namespace InEngine.Core
{
    public class Config
    {
        public static string QueueName { get { return GetString("QueueName", "InEngine:Queue"); } set => throw new NotImplementedException(); }
        public static string RedisHost { get { return GetString("RedisHost", "localhost"); } set => throw new NotImplementedException(); }
        public static int RedisPort { get { return GetInt("RedisDb", 6379); } set => throw new NotImplementedException(); }
        public static int RedisDb { get { return GetInt("RedisDb", 0); } set => throw new NotImplementedException(); }
        public static string RedisPassword { get { return GetString("RedisHist"); } set => throw new NotImplementedException(); }

        public static int GetInt(string key, int defaultValue = 0)
        {
            var val = ConfigurationManager.AppSettings[key];
            return val == null ? defaultValue : Convert.ToInt32(val);
        }

        public static string GetString(string key, string defaultValue = "")
        {
            return ConfigurationManager.AppSettings[key] ?? defaultValue;
        }
    }
}
