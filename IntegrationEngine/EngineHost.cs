using System;

namespace IntegrationEngine
{
    public class EngineHost
    {
        public EngineHost()
        {
            (new EngineConfiguration()).Configure(ContainerSingleton.GetContainer());
        }
    }
}

