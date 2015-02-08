using System;
using System.Collections.Generic;

namespace IntegrationEngine.Model
{
    public interface IIntegrationJobTrigger : IHasStringId, IDispatchable
    {
        int StateId { get; set; }
        string StateDescription { get; }
    }
}
