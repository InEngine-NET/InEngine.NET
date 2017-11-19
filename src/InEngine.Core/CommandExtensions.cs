using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;

namespace InEngine.Core
{
    public static class CommandExtensions
    {
        public static string EncodeCommend(this ICommand command)
        {
            return Convert.ToBase64String(JsonConvert.SerializeObject(command).Compress());
        }

        public static ICommand DecodeCommand(this string encodedCommand, Type type)
        {
            var decoded = Convert.FromBase64String(encodedCommand);
            var uncompressed = decoded.Decompress();
            return JsonConvert.DeserializeObject(uncompressed, type) as ICommand;
        }

        public static byte[] Compress(this string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    StreamCopy.FromTo(msi, gs);
                }
                return mso.ToArray();
            }
        }

        public static string Decompress(this byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    StreamCopy.FromTo(gs, mso);
                }
                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }
    }
}
