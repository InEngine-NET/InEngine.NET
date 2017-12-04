using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using InEngine.Core.Commands;
using Serialize.Linq.Extensions;

namespace InEngine.Core.Queuing
{
    public static class Enqueue
    {
        public static IQueueLifeCycleActions Command(Expression<Action> expressionAction)
        {
            return new QueueLifeCycleActions(new Lambda(expressionAction.ToExpressionNode()));
        }

        public static IQueueLifeCycleActions Command(AbstractCommand command)
        {
            return new QueueLifeCycleActions(command);
        }

        public static IQueueLifeCycleActions Command<T>(T command) where T : AbstractCommand
        {
            return new QueueLifeCycleActions(command);
        }

        public static IQueueLifeCycleActions Commands(IList<AbstractCommand> commands)
        {
            return new QueueLifeCycleActions(new Chain() { Commands = commands });
        }
    }
}
