using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NLog;

namespace InEngine.Core
{
    public class Plugin
    {
        public Assembly Assembly { get; set; }
        public string Name { get { return Assembly.GetName().Name; } }

        public Plugin(Assembly assembly)
        {
            Assembly = assembly;
        }

        public List<T> Make<T>() where T : class, IPluginType
        {
            return Assembly
                .GetTypes()
                .Where(x => x.IsClass && typeof(T).IsAssignableFrom(x))
                .Select(x => Assembly.CreateInstance(x.FullName) as T)
                .ToList();
        }

        public static List<Plugin> Discover<T>() where T : IPluginType
        {
            var discoveredAssemblies = Directory
                .GetFiles(".", "*.dll")
                .Select(x => Assembly.LoadFrom(x));
            var pluginList = new List<Plugin>();
            foreach (var assembly in discoveredAssemblies)
            {
                try
                {
                    if (assembly.GetTypes().Any(y => y.IsClass && typeof(T).IsAssignableFrom(y)))
                        pluginList.Add(new Plugin(assembly));
                }
                catch (Exception exception)
                {
                    LogManager.GetCurrentClassLogger().Error(exception, "Error discovering plugins");
                }
            }
            return pluginList;
        }
    }
}
