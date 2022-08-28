namespace InEngine.Core.Queuing.Clients;

public class RedisClientSettings
{
    public string Host { get; set; }
    public int Port { get; set; }
    public int Database { get; set; }
    public string Password { get; set; }
}