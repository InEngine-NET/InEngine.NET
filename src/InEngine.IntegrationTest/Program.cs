using System;

namespace InEngine.IntegrationTest
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("------------------------------------------------------------");
            Console.WriteLine("Integration Tests");
            Console.WriteLine("------------------------------------------------------------");
            new QueuingTest().Run();
            new SchedulingTest().Run();
        }
    }
}
