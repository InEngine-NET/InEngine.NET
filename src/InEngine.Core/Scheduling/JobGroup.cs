using System.Collections.Generic;

namespace InEngine.Core.Scheduling;

public class JobGroup
{
    public string Name { get; set; }
    public IDictionary<string, JobRegistration> Registrations { get; set; } = new Dictionary<string, JobRegistration>();
}