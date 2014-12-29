using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace IntegrationEngine
{
    public class EngineHost
    {
        public IList<Assembly> AssembliesWithJobs { get; set; }

        public EngineHost(params Assembly[] assembliesWithJobs)
        {
            AssembliesWithJobs = assembliesWithJobs.ToList();
        }

        public void Initialize()
        {
            (new EngineConfiguration()).Configure(ContainerSingleton.GetContainer(), AssembliesWithJobs);
        }
    }
}

