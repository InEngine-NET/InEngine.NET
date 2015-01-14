using System;

namespace IntegrationEngine
{
    internal class Container
    {
        public static T Resolve<T>()
        {
            return ContainerSingleton.GetContainer().Resolve<T>();
        }
        public static T TryResolve<T>()
        {
            return ContainerSingleton.GetContainer().TryResolve<T>();
        }

        public static void Register<T>(T service)
        {
            ContainerSingleton.GetContainer().Register<T>(service);
        }
    }
}
