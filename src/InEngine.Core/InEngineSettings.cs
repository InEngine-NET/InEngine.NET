using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using InEngine.Core.Exceptions;
using InEngine.Core.Queue;
using Microsoft.Extensions.Configuration;

namespace InEngine.Core
{
    public class InEngineSettings
    {
        public QueueSettings Queue { get; set; }

        public static InEngineSettings Make()
        {
            var inEngineSettings = new InEngineSettings();
            new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build()
                .GetSection("InEngine")
                .Bind(inEngineSettings);
            return inEngineSettings;
        }
    }
}
