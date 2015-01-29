using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace IntegrationEngine
{
    public class EngineHost : IDisposable
    {
        EngineHostConfiguration _engineConfiguration;
        public IList<Assembly> AssembliesWithJobs { get; set; }

        public EngineHost(params Assembly[] assembliesWithJobs)
        {
            AssembliesWithJobs = assembliesWithJobs.ToList();
        }

        public void Dispose()
        {
            if (_engineConfiguration != null)
                _engineConfiguration.Dispose();
        }

        public void Initialize()
        {
            _engineConfiguration = new EngineHostConfiguration();
            _engineConfiguration.Configure(AssembliesWithJobs);
        }
    }
}

