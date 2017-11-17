namespace InEngine.Core
{
    public interface IOptions : IPluginType
    {
        string GetUsage(string verb);
    }
}
