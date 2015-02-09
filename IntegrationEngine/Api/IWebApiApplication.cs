using IntegrationEngine.Core.Configuration;
using System;

namespace IntegrationEngine.Api
{
    public interface IWebApiApplication : IDisposable
    {
        IDisposable webApi { get; set; }
        WebApiConfiguration WebApiConfiguration { get; set; }
        void Start();
    }
}
