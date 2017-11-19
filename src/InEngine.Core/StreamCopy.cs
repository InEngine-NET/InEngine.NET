using System.IO;

namespace InEngine.Core
{
    public static class StreamCopy
    {
        public static void FromTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];
            int cnt;
            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
                dest.Write(bytes, 0, cnt);
        }
    }
}
