using System;
using IntegrationEngine.Model;
using Nest;

namespace IntegrationEngine.Model
{
    public class SimpleTrigger : ISimpleTrigger
    {
        public virtual string Id { get; set; }
        public virtual string JobType { get; set; }
        public virtual int RepeatCount { get; set; }
        public virtual TimeSpan RepeatInterval { get; set; }
        public virtual DateTimeOffset StartTimeUtc { get; set; }
        public virtual int StateId { get; set; }
        [ElasticProperty(OptOut = true)]
        public virtual string StateDescription { get { return StateId.GetStateDescription(); } }
    }
}
