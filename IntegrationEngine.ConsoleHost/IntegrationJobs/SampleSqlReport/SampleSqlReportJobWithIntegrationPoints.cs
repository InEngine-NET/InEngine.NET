using IntegrationEngine.Core.Jobs;
using IntegrationEngine.ConsoleHost.IntegrationPoints;
using RazorEngine;
using RazorEngine.Templating;
using System;
using System.Net.Mail;

namespace IntegrationEngine.ConsoleHost.IntegrationJobs.SampleSqlReport
{
    public class SampleSqlReportJobWithIntegrationPoints : IIntegrationJob
    {
        public FooMailClient FooMailClient { get; set; }
        public BarSqlServer BarSqlServer { get; set; }

        public void Run()
        {
            try
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
                FooMailClient.Send(mailMessage);
            }
            catch (Exception exception)
            {
                throw new IntegrationJobRunFailureException("SampleSqlReportJob failed.", exception);
            }
        }
    }
}

