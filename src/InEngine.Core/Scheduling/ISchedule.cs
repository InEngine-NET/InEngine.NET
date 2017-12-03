using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace InEngine.Core.Scheduling
{
    public interface ISchedule
    {
        Occurence Job(AbstractCommand command);
        Occurence Job(Expression<Action> expressionAction);
        Occurence Job(IList<AbstractCommand> commands);
    }
}
