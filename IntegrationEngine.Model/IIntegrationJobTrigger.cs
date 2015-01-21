using System;

namespace IntegrationEngine.Model
{
    public interface IIntegrationJobTrigger : IHasStringId
    {
        string JobType { get; set; }
        int StateId { get; set; }
        string StateDescription { get; }
    }
}
