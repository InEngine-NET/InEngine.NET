using log4net;

namespace IntegrationEngine.Jobs
{
    interface ILogJob : IIntegrationJob
    {
        ILog Log { get; set; }
    }
}
