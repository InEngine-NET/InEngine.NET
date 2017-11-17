using System;
using System.Collections.Generic;
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

        public List<IOptions> MakeOptions()
        {
            return Assembly
                .GetTypes()
                .Where(x => x.IsClass && typeof(IOptions).IsAssignableFrom(x))
                .Select(x => Assembly.CreateInstance(x.FullName) as IOptions)
                .ToList();
        }
    }
}
