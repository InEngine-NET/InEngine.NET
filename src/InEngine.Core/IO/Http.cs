using RestSharp;

namespace InEngine.Core.IO
{
    public class Http
    {
        public string Get(string url)
        {
            return new RestClient(url).Execute(new RestRequest(string.Empty, Method.GET)).Content;
        }
    }
}
