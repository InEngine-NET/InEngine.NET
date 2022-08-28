namespace InEngine.Core.Queuing;

public static class QueueNames
{
    public const string Pending = "Pending";
    public const string InProgress = "InProgress";
    public const string Failed = "Failed";
    
    public const string Primary = "Primary";
    public const string Secondary = "Secondary";
    
    public const string DeadLetter = "DeadLetter";
    public const string Recovery = "Recovery";
}
