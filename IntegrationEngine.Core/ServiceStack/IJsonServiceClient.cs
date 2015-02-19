using IntegrationEngine.Core.IntegrationPoint;
using IntegrationEngine.Core.Configuration;
using ServiceStack.Service;
using System;

namespace IntegrationEngine.Core.ServiceStack
{
    public interface IJsonServiceClient : IServiceClient, IIntegrationPoint<IJsonServiceConfiguration>
    {}
}
