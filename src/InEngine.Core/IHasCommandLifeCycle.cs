using System;
using InEngine.Core.Scheduling;

namespace InEngine.Core
{
    public interface IHasCommandLifeCycle
    {
        CommandLifeCycle CommandLifeCycle { get; set; }
    }
}
