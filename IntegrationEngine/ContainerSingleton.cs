using System;
using Microsoft.Practices.Unity;

namespace IntegrationEngine
{
    internal class ContainerSingleton
    {
        private static IUnityContainer _instance;
        private ContainerSingleton() {}

        public static IUnityContainer GetContainer()
        {
            if (_instance == null)
                _instance = new UnityContainer();
            return _instance;
        }
    }
}

