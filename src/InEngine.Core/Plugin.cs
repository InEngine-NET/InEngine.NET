using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using InEngine.Core.Exceptions;
using NLog;

namespace InEngine.Core
{
    public class Plugin
    {
        public Assembly Assembly { get; set; }
        public string Name { get { return Assembly.GetName().Name; } }
        public string Version { get { return Assembly.GetName().Version.ToString(); } }

        public Plugin(Assembly assembly)
        {
            Assembly = assembly;
        }

        public static Plugin LoadFrom(string assemblyPath)
        {
            var path = Path.Combine(InEngineSettings.BasePath, assemblyPath);
            try
            {
                return new Plugin(Assembly.LoadFrom(path));   
            }
            catch (Exception exception)
            {
                throw new PluginNotFoundException($"Plugin not found at {path}", exception);
            }
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
            var logger = LogManager.GetCurrentClassLogger();
            var discoveredAssemblies = InEngineSettings.Make().PluginDirectories.SelectMany(x => {
                if (!Directory.Exists(x)) {
                    logger.Warn("Plugin directory does not exist: " + x);
                    return new List<Assembly>();
                }
                return Directory.GetFiles(x, "*.dll").Select(y => Assembly.LoadFrom(y));                
            });
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
                    logger.Error(exception, "Error discovering plugins");
                }
            }
            return pluginList.OrderBy(x => x.Name).ToList();
        }

        public ICommand CreateCommandInstance(string fullCommandName)
        {
            return Assembly.CreateInstance(fullCommandName) as ICommand;   
        }
    }
}
