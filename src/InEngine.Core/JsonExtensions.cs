using Newtonsoft.Json;

namespace InEngine.Core
{
    public static class JsonExtensions
    {
        public static string SerializeToJson<T>(this T message, bool compress = false) where T : class
        {
            var serializedMessage = JsonConvert.SerializeObject(message);
            return compress ? serializedMessage.Compress() : serializedMessage;
        }

        public static T DeserializeFromJson<T>(this string payload, bool decompress = false)
        {
            var serializedMessage = decompress ? payload.Decompress() : payload;
            return JsonConvert.DeserializeObject<T>(serializedMessage);
        }
    }
}
