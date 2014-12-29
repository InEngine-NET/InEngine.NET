using System;
using System.Reflection;

namespace IntegrationEngine.ConsoleHost
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            (new EngineHost(typeof(MainClass).Assembly)).Initialize();
        }
    }
}
