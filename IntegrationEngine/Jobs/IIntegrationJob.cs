using System;

namespace IntegrationEngine.Jobs
{
    public interface IIntegrationJob
    {
        TimeSpan Interval { get; set; }
        DateTimeOffset StartTimeUtc { get; set; }
        void Run();
    }
}
