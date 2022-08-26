using System;
using InEngine.Core.Scheduling;

namespace InEngine.Core.LifeCycle;

public interface IHasCommandLifeCycle
{
    CommandLifeCycle CommandLifeCycle { get; set; }
}