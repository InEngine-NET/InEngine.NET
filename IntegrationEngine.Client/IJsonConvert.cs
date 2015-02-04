using System;

namespace IntegrationEngine.Client
{
    public interface IJsonConvert
    {
        T DeserializeObject<T>(string value);
        string SerializeObject(object value);
    }
}

