using System;
using Quartz;
using Quartz.Impl;
using System.Threading;
using Quartz.API;
using TryQuartz.Jobs;
using RabbitMQ.Client;
using System.Text;
using Funq;

namespace TryQuartz
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            (new ProgramConfiguration()).Configure(ContainerSingleton.GetContainer());
        }
    }
}
