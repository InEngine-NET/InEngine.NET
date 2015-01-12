using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace IntegrationEngine
{
    public class EngineHost
    {
        EngineHostConfiguration _engineConfiguration;
        public IList<Assembly> AssembliesWithJobs { get; set; }

        public EngineHost(params Assembly[] assembliesWithJobs)
        {
            AssembliesWithJobs = assembliesWithJobs.ToList();
        }

        ~EngineHost()
        {
            if (_engineConfiguration != null)
                _engineConfiguration.Shutdown();
        }

        public void Initialize()
        {
            _engineConfiguration = new EngineHostConfiguration();
            _engineConfiguration.Configure(AssembliesWithJobs);
        }
    }
}

