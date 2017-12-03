namespace InEngine.Core.Queuing.Clients.Database
{
    public static class CommandEnvelopeStatus
    {
        public static string Pending { get => "Pending"; }
        public static string InProgress { get => "InProgress"; }
        public static string Failed { get => "Failed"; }
        public static string Completed { get => "Completed"; }
    }
}
