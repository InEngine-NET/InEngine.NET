using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;

namespace InEngine.Core
{
    public static class CompressionExtensions
    {
        public static string Compress(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return String.Empty;
            var buffer = Encoding.UTF8.GetBytes(text);
            var memoryStream = new MemoryStream();
            using (GZipStream zip = new GZipStream(memoryStream, CompressionMode.Compress, true))
            {
                zip.Write(buffer, 0, buffer.Length);
            }
            memoryStream.Position = 0;
            var outStream = new MemoryStream();
            var compressedBytes = new byte[memoryStream.Length];
            memoryStream.Read(compressedBytes, 0, compressedBytes.Length);
            var gzBuffer = new byte[compressedBytes.Length + 4];
            Buffer.BlockCopy(compressedBytes, 0, gzBuffer, 4, compressedBytes.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gzBuffer, 0, 4);
            return Convert.ToBase64String(gzBuffer);
        }

        public static string Decompress(this string compressedText)
        {
            if (string.IsNullOrWhiteSpace(compressedText))
                return String.Empty;
            var gzBuffer = Convert.FromBase64String(compressedText);
            using (MemoryStream memoryStream = new MemoryStream())
            {
                int messageLength = BitConverter.ToInt32(gzBuffer, 0);
                memoryStream.Write(gzBuffer, 4, gzBuffer.Length - 4);
                var buffer = new byte[messageLength];
                memoryStream.Position = 0;
                using (GZipStream zipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    zipStream.Read(buffer, 0, buffer.Length);
                }
                return Encoding.UTF8.GetString(buffer);
            }
        }
    }
}
