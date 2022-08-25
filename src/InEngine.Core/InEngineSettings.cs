using System;
using System.Collections.Generic;
using System.IO;
using InEngine.Core.IO;
using InEngine.Core.Queuing;
using Microsoft.Extensions.Configuration;

namespace InEngine.Core
{
    public class InEngineSettings
    {
        public static string BasePath { get; set; } = Directory.GetCurrentDirectory();
        public static string ConfigurationFile { get; set; } = "appsettings.json";
        public IDictionary<string, string> Plugins { get; set; } = new Dictionary<string, string>();
        public QueueSettings Queue { get; set; } = new();
        public MailSettings Mail { get; set; } = new();
        public IDictionary<string, string> ExecWhitelist { get; set; } = new Dictionary<string, string>();

        public static InEngineSettings Make()
        {
            var inEngineSettings = new InEngineSettings();
            try
            {
                new ConfigurationBuilder()
                    .SetBasePath(BasePath)
                    .AddJsonFile(ConfigurationFile)
                    .Build()
                    .GetSection("InEngine")
                    .Bind(inEngineSettings);
            }
            catch (FileNotFoundException exception){
                new Write().Error(exception.Message);;
                Environment.Exit(ExitCodes.Fail);
            }
            catch (Exception exception)
            {
                new Write().Error(exception.Message);
                Environment.Exit(ExitCodes.Fail);                
            }
            return inEngineSettings;
        }
    }
}
