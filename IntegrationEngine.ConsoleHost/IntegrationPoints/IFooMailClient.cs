using System;
using IntegrationEngine.Core.Mail;
using IntegrationEngine.Core.Configuration;

namespace IntegrationEngine.ConsoleHost.IntegrationPoints
{
    //[IntegrationPointConfiguration("FooMailClient")]
    public interface IFooMailClient : IMailClient
    {
    }
}
