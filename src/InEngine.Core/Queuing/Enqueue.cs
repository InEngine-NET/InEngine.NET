using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using InEngine.Core.Commands;
using InEngine.Core.Queuing.LifeCycle;
using Serialize.Linq.Extensions;

namespace InEngine.Core.Queuing
{
    public static class Enqueue
    {
        public static IQueueLifeCycleBuilder Command(Expression<Action> expressionAction)
        {
            return new QueueLifeCycleBuilder(new Lambda(expressionAction.ToExpressionNode()));
        }

        public static IQueueLifeCycleBuilder Command(AbstractCommand command)
        {
            return new QueueLifeCycleBuilder(command);
        }

        public static IQueueLifeCycleBuilder Command<T>(T command) where T : AbstractCommand
        {
            return new QueueLifeCycleBuilder(command);
        }

        public static IQueueLifeCycleBuilder Commands(IList<AbstractCommand> commands)
        {
            return new QueueLifeCycleBuilder(new Chain() { Commands = commands });
        }
    }
}
