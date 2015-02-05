using IntegrationEngine.Core.Jobs;
using IntegrationEngine.Core.Mail;
using RazorEngine;
using RazorEngine.Templating;
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

            // Pass into Razor engine
            string template = "Created on <strong>@Model.Created</strong> with <strong>@Model.Data.Count</strong> records.";
            var html = Engine.Razor.RunCompile(template, "template-01", typeof(SampleReport), report);

            // Send Mail
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
