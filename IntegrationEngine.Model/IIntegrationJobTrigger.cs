using System;
using System.Collections.Generic;

namespace IntegrationEngine.Model
{
    public interface IIntegrationJobTrigger : IHasStringId, IHasParameters
    {
        string JobType { get; set; }
        int StateId { get; set; }
        string StateDescription { get; }
    }
}
