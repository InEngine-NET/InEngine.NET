using System;
using IntegrationEngine.Core.Mail;
using IntegrationEngine.Core.Configuration;

namespace IntegrationServer.IntegrationPoints
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
