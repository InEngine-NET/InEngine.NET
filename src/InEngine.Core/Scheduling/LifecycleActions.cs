using System;
namespace InEngine.Core.Scheduling
{
    public class LifecycleActions
    {
        public JobRegistration JobRegistration { get; set; }
        public Action<AbstractCommand> BeforeAction { get; set; }
        public Action<AbstractCommand> AfterAction { get; set; }

        public LifecycleActions Before(Action<AbstractCommand> action)
        {
            JobRegistration.Command.LifecycleActions.BeforeAction = action;
            return this;
        }

        public LifecycleActions After(Action<AbstractCommand> action)
        {
            JobRegistration.Command.LifecycleActions.AfterAction = action;
            return this;
        }
    }
}
