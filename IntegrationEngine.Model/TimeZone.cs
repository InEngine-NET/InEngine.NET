using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Model
{
    public class TimeZone : ITimeZone
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }

        public TimeZone()
        {}

        public TimeZone(TimeZoneInfo timeZoneInfo) 
            : this()
        {
            Id = timeZoneInfo.Id;
            DisplayName = timeZoneInfo.DisplayName;
        }
    }
}
