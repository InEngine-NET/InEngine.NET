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

        public override string ToString()
        {
            return string.Format("[SimpleTrigger: Id={0}, JobType={1}, RepeatCount={2}, RepeatInterval={3}, StartTimeUtc={4}, StateId={5}, StateDescription={6}]", Id, JobType, RepeatCount, RepeatInterval, StartTimeUtc, StateId, StateDescription);
        }
    }
}
