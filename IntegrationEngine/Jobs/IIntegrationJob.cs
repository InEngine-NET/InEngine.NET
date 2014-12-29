using System;

namespace IntegrationEngine
{
    public interface IIntegrationJob
    {
        TimeSpan Interval { get; set; }
        DateTimeOffset StartTimeUtc { get; set; }
        void Run();
    }
}
