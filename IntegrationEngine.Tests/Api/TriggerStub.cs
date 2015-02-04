using IntegrationEngine.Model;
using System;
using System.Collections.Generic;

namespace IntegrationEngine.Tests.Api
{
    public class TriggerStub : IIntegrationJobTrigger
    {
        public string JobType { get; set; }
        public int StateId { get; set; }
        public string StateDescription { get { return StateId.ToString(); } }
        public IDictionary<string, string> Parameters { get; set; }
        public string Id { get; set; }

        public TriggerStub()
        {
        }
    }
}

