using Newtonsoft.Json;

namespace InEngine.Core
{
    public static class JsonExtensions
    {
        public static string SerializeToJson<T>(this T message) where T : class
        {
            return JsonConvert.SerializeObject(message);
        }

        public static T DeserializeFromJson<T>(this string payload)
        {
            return JsonConvert.DeserializeObject<T>(payload);
        }
    }
}
