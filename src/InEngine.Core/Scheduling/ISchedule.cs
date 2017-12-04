using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Quartz;

namespace InEngine.Core.Scheduling
{
    public interface ISchedule
    {
        Occurence Command(AbstractCommand command);
        Occurence Command(Expression<Action> expressionAction);
        Occurence Command(IList<AbstractCommand> commands);
    }
}
