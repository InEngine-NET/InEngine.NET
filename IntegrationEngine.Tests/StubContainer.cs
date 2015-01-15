using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Tests
{
    public class StubContainer : IUnityContainer
    {
        public IUnityContainer AddExtension(UnityContainerExtension extension)
        {
            throw new NotImplementedException();
        }

        public object BuildUp(Type t, object existing, string name, params ResolverOverride[] resolverOverrides)
        {
            throw new NotImplementedException();
        }

        public object Configure(Type configurationInterface)
        {
            throw new NotImplementedException();
        }

        public IUnityContainer CreateChildContainer()
        {
            throw new NotImplementedException();
        }

        public IUnityContainer Parent
        {
            get { throw new NotImplementedException(); }
        }

        public IUnityContainer RegisterInstance(Type t, string name, object instance, LifetimeManager lifetime)
        {
            throw new NotImplementedException();
        }

        public virtual IUnityContainer RegisterInstance<TInterface>(TInterface instance)
        {
            return this;
        }

        public IUnityContainer RegisterType(Type from, Type to, string name, LifetimeManager lifetimeManager, params InjectionMember[] injectionMembers)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ContainerRegistration> Registrations
        {
            get { throw new NotImplementedException(); }
        }

        public IUnityContainer RemoveAllExtensions()
        {
            throw new NotImplementedException();
        }

        public object Resolve(Type t, string name, params ResolverOverride[] resolverOverrides)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<object> ResolveAll(Type t, params ResolverOverride[] resolverOverrides)
        {
            throw new NotImplementedException();
        }

        public void Teardown(object o)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
