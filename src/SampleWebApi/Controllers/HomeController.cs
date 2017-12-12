using System.Web.Http;
using InEngine.Commands.Sample;
using InEngine.Core.Queuing;

namespace SampleWebApi.Controllers
{
    public class HomeController : ApiController
    {
        public IHttpActionResult Get()
        {
            Enqueue.Command(new SayHello()).Dispatch();

            return Ok("ok");
        }
    }
}
