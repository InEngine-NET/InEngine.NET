namespace InEngine.Core.Queuing.Clients.Database
{
    public class CommandEnvelopeModel : CommandEnvelope
    {
        public string Status { get; set; }
        public string QueueName { get; set; }
    }
}
