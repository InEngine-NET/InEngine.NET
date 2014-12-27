using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using Nest;
using TryQuartz.DataTransfer;

namespace TryQuartz
{
    public class RScriptLauncher
    {
        public IElasticClient ElasticClient { get; set; }

        public RScriptLauncher()
        {
            ElasticClient = ContainerSingleton.GetContainer().Resolve<IElasticClient>();
        }

        public void Run()
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = "/usr/bin/Rscript";
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.Arguments = "RScripts/sample.R";
            startInfo.RedirectStandardOutput = true;

            var scriptOutput = "";
            using (var process = System.Diagnostics.Process.Start(startInfo))
            {
                scriptOutput = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }

            // Write result to Elasticsearch.
            var carReport = new CarReport() { 
                Created = DateTime.UtcNow,
                Data = JsonConvert.DeserializeObject<List<Car>>(scriptOutput),
            };

            ElasticClient.Index(carReport, x => x.Type(carReport.GetType().Name));
        }
    }
}

