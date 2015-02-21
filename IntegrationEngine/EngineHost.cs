using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IntegrationEngine
{
    public class EngineHost : IDisposable
    {
        EngineHostCompositionRoot engineHostCompositionRoot;
        public IUnityContainer Container { get { return engineHostCompositionRoot.Container; } }
        public IList<Assembly> AssembliesWithJobs { get; set; }
       
        public EngineHost(params Assembly[] assembliesWithJobs)
        {
            AssembliesWithJobs = assembliesWithJobs.ToList();
        }

        public void Dispose()
        {
            if (engineHostCompositionRoot != null)
                engineHostCompositionRoot.Dispose();
        }

        public void Initialize(bool isThreadedListenerEnabled = true, 
                               bool isSchedulerEnabled = true, 
                               bool isWebApiEnabled = true)
        {
            engineHostCompositionRoot = new EngineHostCompositionRoot(AssembliesWithJobs) {
                IsWebApiEnabled = isWebApiEnabled,
                IsSchedulerEnabled = isSchedulerEnabled,
                IsThreadedListenerEnabled = isThreadedListenerEnabled,
            };
            engineHostCompositionRoot.Configure();
        }
    }
}

