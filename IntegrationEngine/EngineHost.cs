using System;

namespace IntegrationEngine
{
    public class EngineHost
    {
        public EngineHost()
        {
        }

        public void Initialize()
        {
            (new EngineConfiguration()).Configure(ContainerSingleton.GetContainer());
        }
    }
}

