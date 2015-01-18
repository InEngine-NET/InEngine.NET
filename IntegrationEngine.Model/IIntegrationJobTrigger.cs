using System;

namespace IntegrationEngine.Model
{
    public interface IIntegrationJobTrigger : IHasStringId, IHasStateId
    {
        string JobType { get; set; }
    }
}
