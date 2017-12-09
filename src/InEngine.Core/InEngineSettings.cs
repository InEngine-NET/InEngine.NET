using System;
using System.Collections.Generic;
using System.IO;
using InEngine.Core.Commands;
using InEngine.Core.IO;
using InEngine.Core.Queuing;
using Microsoft.Extensions.Configuration;

namespace InEngine.Core
{
    public class InEngineSettings
    {
        public static string BasePath { get; set; } = Directory.GetCurrentDirectory();
        public static string ConfigurationFile { get; set; } = "appsettings.json";
        public List<string> Plugins { get; set; } = new List<string>();
        public QueueSettings Queue { get; set; } = new QueueSettings();
        public MailSettings Mail { get; set; } = new MailSettings();
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
                Environment.Exit(ExitCodes.fail);
            }
            catch (Exception exception)
            {
                new Write().Error(exception.Message);
                Environment.Exit(ExitCodes.fail);                
            }
            return inEngineSettings;
        }
    }
}
