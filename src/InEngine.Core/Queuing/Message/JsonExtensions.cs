using Newtonsoft.Json;

namespace InEngine.Core.Queuing.Message
{
    static class JsonExtensions
    {
        public static string SerializeToJson<T>(this T message, bool compress = false) where T : class
        {
            var serializedMessage = JsonConvert.SerializeObject(
                message, 
                Formatting.Indented,
                new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Objects }
            );
            return compress ? serializedMessage.Compress() : serializedMessage;
        }

        public static T DeserializeFromJson<T>(this string payload, bool decompress = false)
        {
            var serializedMessage = decompress ? payload.Decompress() : payload;
            return JsonConvert.DeserializeObject<T>(
                serializedMessage, 
                new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Objects }
            );
        }
    }
}
