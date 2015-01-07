using IntegrationEngine.Core.Jobs;
using IntegrationEngine.Core.Mail;
using RazorEngine;
using System;
using System.Net.Mail;

namespace IntegrationEngine.ConsoleHost.IntegrationJobs.SampleSqlReport
{
    public class SampleSqlReportJob : SqlJob
    {
        public override void Run()
        {
            var report = new SampleReport() {
                Created = DateTime.Now,
                Data = new System.Collections.Generic.List<SampleDatum>(),
                //Data = RunQuery<SampleDatum>(),
            };

            // Write result to Elasticsearch

            // Pass into Razor engine
            string template = "Created on <strong>@Model.Created</strong> with <strong>@Model.Data.Count</strong> records.";
            Razor.Compile<SampleReport>(template, "template-01");
            var html = Razor.Run("template-01", report);
            //Console.WriteLine(result.ToString());
             
            // Write result to Elasticsearch
            // Send Mail
            // How to get recipients?
            var mailMessage = new MailMessage();
            mailMessage.To.Add("ethanhann@gmail.com");
            mailMessage.Subject = "Sample SQL Report";
            mailMessage.From = new MailAddress("root@localhost");
            mailMessage.Body = html;
            mailMessage.IsBodyHtml = true;
            MailClient.Send(mailMessage);
        }
    }
}
