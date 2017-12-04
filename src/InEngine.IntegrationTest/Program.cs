using System;

namespace InEngine.IntegrationTest
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("------------------------------------------------------------");
            Console.WriteLine("Queuing Integration Tests");
            Console.WriteLine("------------------------------------------------------------");
            new QueuingTest().Run();

            Console.WriteLine("------------------------------------------------------------");
            Console.WriteLine("Scheduling Integration Tests");
            Console.WriteLine("------------------------------------------------------------");
            new SchedulingTest().Run();
        }
    }   
}
