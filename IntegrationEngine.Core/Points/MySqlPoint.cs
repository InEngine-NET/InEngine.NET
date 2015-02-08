using System;

namespace IntegrationEngine.Core.Points
{
    public class MySqlPoint : IMySqlPoint
    {
        public string HostName { get; set; }
        public uint Port { get; set; }
        public string DatabaseName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public MySqlPoint()
        {
        }
    }
}

