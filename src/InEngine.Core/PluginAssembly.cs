using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine;
using InEngine.Core.Exceptions;

namespace InEngine.Core
{
    public class PluginAssembly
    {
        public Assembly Assembly { get; set; }
        public string Name { get { return Assembly.GetName().Name; } }
        public string Version { get { return Assembly.GetName().Version.ToString(); } }
        public List<AbstractPlugin> Plugins { get; set; }
        public PluginAssembly(Assembly assembly)
        {
            Assembly = assembly;
            Plugins = Make<AbstractPlugin>();
        }

        public static PluginAssembly LoadFrom(string assemblyPath)
        {
            var path = Path.Combine(InEngineSettings.BasePath, assemblyPath);
            try
            {
                return new PluginAssembly(Assembly.LoadFrom(path));   
            }
            catch (Exception exception)
            {
                throw new PluginNotFoundException($"Plugin not found at {path}", exception);
            }
        }

        public List<T> Make<T>() where T : class, IPlugin
        {
            return Assembly
                .GetTypes()
                .Where(x => x.IsClass && typeof(T).IsAssignableFrom(x))
                .Select(x => Assembly.CreateInstance(x.FullName) as T)
                .ToList();
        }

        public static List<PluginAssembly> Load<T>() where T : IPlugin
        {
            var pluginList = new List<PluginAssembly>();
            try
            {
                pluginList.Add(new PluginAssembly(Assembly.GetExecutingAssembly()));    
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
                        pluginList.Add(new PluginAssembly(assembly));
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
