using System;
using FunqContainer = Funq.Container;

namespace IntegrationEngine
{
    public class ContainerSingleton
    {
        private static FunqContainer _instance;
        private ContainerSingleton() {}

        public static FunqContainer GetContainer()
        {
            if (_instance == null)
                _instance = new FunqContainer();
            return _instance;
        }
    }
}

