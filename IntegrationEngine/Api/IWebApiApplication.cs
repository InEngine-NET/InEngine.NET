using IntegrationEngine.Configuration;
using System;

namespace IntegrationEngine.Api
{
    public interface IWebApiApplication
    {
        IDisposable webApi { get; set; }
        WebApiConfiguration WebApiConfiguration { get; set; }
        void Start();
        void Stop();
    }
}
