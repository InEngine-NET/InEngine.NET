using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using CommandLine;
using InEngine.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace InEngine.Core;

public class PluginAssembly
{
    private ILogger Log { get; set; } = LogManager.GetLogger<PluginAssembly>();

    public Assembly Assembly { get; set; }
    public string Name => Assembly.GetName().Name;
    public string Version => Assembly.GetName().Version.ToString();
    public List<AbstractPlugin> Plugins { get; set; }
    public static volatile bool IsAssemblyResolverRegistered;
    public static readonly Mutex AssemblyResolverLock = new();

    public PluginAssembly(Assembly assembly)
    {
        Assembly = assembly;
        Plugins = Make<AbstractPlugin>();
    }

    public static PluginAssembly LoadFrom(string pluginName)
    {
        RegisterPluginAssemblyResolver();
        var path = MakeFullPluginAssemblyPath(pluginName);
        try
        {
            return new PluginAssembly(Assembly.LoadFrom(path));
        }
        catch (Exception exception)
        {
            const string message = $"Plugin not found";
            LogManager.GetLogger<PluginAssembly>().LogError(exception, message);
            throw new PluginNotFoundException(message, exception);
        }
    }

    public static List<PluginAssembly> Load<T>(bool shouldLoadCorePlugin = true) where T : IPlugin
    {
        RegisterPluginAssemblyResolver();
        var pluginList = new List<PluginAssembly>();

        try
        {
            if (shouldLoadCorePlugin)
                pluginList.Add(new PluginAssembly(Assembly.GetExecutingAssembly()));
        }
        catch (Exception exception)
        {
            const string message = "Could not load InEngine.Core plugin.";
            LogManager.GetLogger<PluginAssembly>().LogError(exception, message);
            throw new PluginNotFoundException(message, exception);
        }

        var assemblies = InEngineSettings
            .Make()
            .Plugins
            .Select(x => Assembly.LoadFrom(Path.Combine(x.Value, $"{x.Key}.dll")));

        foreach (var assembly in assemblies)
        {
            try
            {
                if (assembly.GetTypes().Any(x => x.IsClass && typeof(T).IsAssignableFrom(x) && !x.IsAbstract))
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

    public static void RegisterPluginAssemblyResolver()
    {
        AssemblyResolverLock.WaitOne();
        if (!IsAssemblyResolverRegistered)
        {
            IsAssemblyResolverRegistered = true;
            AppDomain.CurrentDomain.AssemblyResolve += LoadPluginEventHandler;
        }

        AssemblyResolverLock.ReleaseMutex();
    }

    private static Assembly LoadPluginEventHandler(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name;
        if (assemblyName == null) 
            return null;
        var pluginName = assemblyName[..^4];
        var assemblyPath = MakeFullPluginAssemblyPath(pluginName);
        return File.Exists(assemblyPath) ? Assembly.LoadFrom(assemblyPath) : null;
    }

    private static string MakeFullPluginAssemblyPath(string pluginName)
    {
        var plugins = InEngineSettings.Make().Plugins;
        var isCorePlugin = Assembly.GetCallingAssembly().GetName().Name == pluginName;
        if (!isCorePlugin && !plugins.ContainsKey(pluginName))
            throw new PluginNotRegisteredException(pluginName);
        return Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty,
            isCorePlugin ? "" : plugins[pluginName],
            $"{pluginName}.dll"
        );
    }

    public List<T> Make<T>() where T : class, IPlugin
    {
        return Assembly
            .GetTypes()
            .Where(x => x.IsClass &&
                        typeof(T).IsAssignableFrom(x) &&
                        !x.IsAbstract &&
                        !string.IsNullOrWhiteSpace(x.FullName)
            )
            .Select(x => Assembly.CreateInstance(x.FullName) as T)
            .ToList();
    }

    public AbstractCommand CreateCommandFromClass(string fullCommandName) =>
        Assembly.CreateInstance(fullCommandName) as AbstractCommand;

    public AbstractCommand CreateCommandFromVerb(string verbName)
    {
        var commandClassNames = new List<string>();
        var optionsList = Make<AbstractPlugin>();

        foreach (var options in optionsList)
        foreach (var property in options.GetType().GetProperties())
        foreach (var attribute in property.GetCustomAttributes(true))
            if (attribute is VerbOptionAttribute optionAttribute && optionAttribute.LongName == verbName)
                commandClassNames.Add(property.PropertyType.FullName);

        var commandCount = commandClassNames.Count();
        if (commandCount > 1)
            throw new AmbiguousCommandException(verbName);
        if (commandCount == 0)
            throw new CommandNotFoundException(verbName);
        return Assembly.CreateInstance(commandClassNames.First()) as AbstractCommand;
    }

    public Type GetCommandType(string commandClassName) => Assembly.GetType(commandClassName);
}