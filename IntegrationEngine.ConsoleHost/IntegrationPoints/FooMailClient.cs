using System;
using IntegrationEngine.Core.Mail;
using IntegrationEngine.Core.Configuration;

namespace IntegrationEngine.ConsoleHost.IntegrationPoints
{
    //[IntegrationPointConfiguration("FooMailClient")]
    public class FooMailClient : MailClient
    {
        public FooMailClient(IMailConfiguration mailConfiguration)
        {
            MailConfiguration = mailConfiguration;
        }
    }
}
