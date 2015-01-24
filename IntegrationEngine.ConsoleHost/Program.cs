using System;
using System.Reflection;

namespace IntegrationEngine.ConsoleHost
{
    public class MainClass
    {
        public static void Main(string[] args)
        {
            (new EngineHost(typeof(MainClass).Assembly)).Initialize();
        }
    }
}
