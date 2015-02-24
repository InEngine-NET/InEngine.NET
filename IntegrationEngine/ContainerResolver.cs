using System;
using System.Web.Http.Dependencies;
using Microsoft.Practices.Unity;
using System.Collections.Generic;

namespace IntegrationEngine
{
    public class ContainerResolver : IDependencyResolver
    {
        protected IUnityContainer container;

        public ContainerResolver()
        {}

        public ContainerResolver(IUnityContainer container) : this()
        {
            if (container == null)
                throw new ArgumentNullException("container");
            this.container = container;
        }

        public object GetService(Type serviceType)
        {
            try
            {
                return container.Resolve(serviceType);
            }
            catch (ResolutionFailedException)
            {
                return null;
            }
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            try
            {
                return container.ResolveAll(serviceType);
            }
            catch (ResolutionFailedException)
            {
                return new List<object>();
            }
        }

        public IDependencyScope BeginScope()
        {
            return new ContainerResolver(container.CreateChildContainer());
        }

        public void Dispose()
        {
            container.Dispose();
        }
    }
}

