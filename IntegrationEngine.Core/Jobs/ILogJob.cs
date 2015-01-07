using log4net;

namespace IntegrationEngine.Core.Jobs
{
    public interface ILogJob : IIntegrationJob
    {
        ILog Log { get; set; }
    }
}
