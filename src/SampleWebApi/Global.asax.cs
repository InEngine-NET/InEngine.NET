using System.Web;
using System.Web.Http;
using InEngine.Core.Queuing;
using InEngine.Core.Scheduling;

namespace SampleWebApi
{
    public class Global : HttpApplication
    {
        public SuperScheduler SuperScheduler { get; set; }
        public Dequeue Dequeue { get; set; }

        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);

            SuperScheduler = new SuperScheduler();
            SuperScheduler.Initialize();
            SuperScheduler.Start();

            Dequeue = new Dequeue();
            StartQueueServerAsync();
        }

        async void StartQueueServerAsync() => await Dequeue.StartAsync();

        protected void Application_End()
        {
            SuperScheduler?.Shutdown();
        }
    }
}
