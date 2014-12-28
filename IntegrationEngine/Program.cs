using System;

namespace IntegrationEngine
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            (new ProgramConfiguration()).Configure(ContainerSingleton.GetContainer());
        }
    }
}
