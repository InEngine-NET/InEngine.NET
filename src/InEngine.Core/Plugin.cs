using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine;
using InEngine.Core.Exceptions;

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

        public static List<Plugin> Load<T>() where T : IPluginType
        {
            var pluginList = new List<Plugin>();
            try
            {
                pluginList.Add(new Plugin(Assembly.GetExecutingAssembly()));    
            } 
            catch (Exception exception)
            {
                throw new PluginNotFoundException("Could not load InEngine.Core plugin.", exception);
            }
                    
            var assemblies = InEngineSettings
                .Make()
                .Plugins
                .Select(x => Assembly.LoadFrom($"{x}.dll"));
            foreach (var assembly in assemblies)
            {
                try
                {
                    if (assembly.GetTypes().Any(y => y.IsClass && typeof(T).IsAssignableFrom(y)))
                        pluginList.Add(new Plugin(assembly));
                }
                catch (Exception exception)
                {
                    throw new PluginNotFoundException($"Could not load {assembly.GetName().Name} plugin.", exception);
                }
            }
            if (!pluginList.Any())
                throw new PluginNotFoundException("There are no plugins available.");
            return pluginList.OrderBy(x => x.Name).ToList();
        }

        public AbstractCommand CreateCommandFromClass(string fullCommandName)
        {
            return Assembly.CreateInstance(fullCommandName) as AbstractCommand;
        }

        public AbstractCommand CreateCommandFromVerb(string verbName)
        {
            var commandClassNames = new List<string>();
            var optionsList = Make<AbstractPlugin>();

            foreach (var options in optionsList)
                foreach (var property in options.GetType().GetProperties())
                    foreach (var attribute in property.GetCustomAttributes(true))
                        if (attribute is VerbOptionAttribute && (attribute as VerbOptionAttribute).LongName == verbName)
                            commandClassNames.Add(property.PropertyType.FullName);

            var commandCount = commandClassNames.Count();
            if (commandCount > 1)
                throw new AmbiguousCommandException(verbName);
            if (commandCount == 0)
                throw new CommandNotFoundException(verbName);
            return Assembly.CreateInstance(commandClassNames.First()) as AbstractCommand;   
        }
    }
}
