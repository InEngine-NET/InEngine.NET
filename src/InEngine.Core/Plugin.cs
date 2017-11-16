using System;
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
    }
}
