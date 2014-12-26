using System;
using Quartz;
using Funq;

namespace TryQuartz
{
    public class ContainerAware
    {
        public IMessageQueueClient MessageQueueClient { get; set; }
        public Container Container { get; set; }

        public ContainerAware()
        {
            Container = ContainerSingleton.GetContainer();
            MessageQueueClient = Container.Resolve<IMessageQueueClient>();
        }
    }
}

