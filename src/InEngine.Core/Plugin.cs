using System;
using System.Linq;
using System.Reflection;

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

        public IOptions MakeOptions()
        {
            var optionType = Assembly.GetTypes().FirstOrDefault(x => x.IsClass && typeof(IOptions).IsAssignableFrom(x));
            if (optionType == null)
                Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
            return Assembly.CreateInstance(optionType.FullName) as IOptions;
        }
    }
}
