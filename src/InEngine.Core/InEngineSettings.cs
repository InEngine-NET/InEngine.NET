using System.Collections.Generic;
using System.IO;
using InEngine.Core.Queue;
using Microsoft.Extensions.Configuration;

namespace InEngine.Core
{
    public class InEngineSettings
    {
        public static string BasePath { get; set; } = Directory.GetCurrentDirectory();
        public static string ConfigurationFile { get; set; } = "appsettings.json";
        public List<string> PluginDirectories { get; set; }
        public QueueSettings Queue { get; set; }

        public static InEngineSettings Make()
        {
            var inEngineSettings = new InEngineSettings();
            new ConfigurationBuilder()
                .SetBasePath(BasePath)
                .AddJsonFile(ConfigurationFile)
                .Build()
                .GetSection("InEngine")
                .Bind(inEngineSettings);
            return inEngineSettings;
        }
    }
}
