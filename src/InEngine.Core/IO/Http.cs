using RestSharp;

namespace InEngine.Core.IO
{
    public static class Http
    {
        public static string Get(string url)
        {
            return new RestClient(url).Execute(new RestRequest(string.Empty, Method.GET)).Content;
        }
    }
}
