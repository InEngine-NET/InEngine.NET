using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using InEngine.Core.Commands;
using InEngine.Core.Queuing.LifeCycle;
using Serialize.Linq.Extensions;

namespace InEngine.Core.Queuing;

public static class Enqueue
{
    public static IQueueLifeCycleBuilder Command(Expression<Action> expressionAction)
    {
        var settings = InEngineSettings.Make();
        return new QueueLifeCycleBuilder(new Lambda(expressionAction.ToExpressionNode())) {
            QueueSettings = settings.Queue,
            MailSettings = settings.Mail,
        };
    }

    public static IQueueLifeCycleBuilder Command(AbstractCommand command)
    {
        var settings = InEngineSettings.Make();
        return new QueueLifeCycleBuilder(command) {
            QueueSettings = settings.Queue,
            MailSettings = settings.Mail,
        };
    }

    public static IQueueLifeCycleBuilder Command<T>(T command) where T : AbstractCommand
    {
        var settings = InEngineSettings.Make();
        return new QueueLifeCycleBuilder(command) {
            QueueSettings = settings.Queue,
            MailSettings = settings.Mail,
        };
    }

    public static IQueueLifeCycleBuilder Commands(IList<AbstractCommand> commands)
    {
        var settings = InEngineSettings.Make();
        return new QueueLifeCycleBuilder(new Chain() { Commands = commands }) {
            QueueSettings = settings.Queue,
            MailSettings = settings.Mail,
        };
    }
}