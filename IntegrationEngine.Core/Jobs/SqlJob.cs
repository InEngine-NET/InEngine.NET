using IntegrationEngine.Core.Mail;
using IntegrationEngine.Core.Reports;
using IntegrationEngine.Core.Storage;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Core.Jobs
{
    public class SqlJob : ISqlJob, IMailJob, ILogJob
    {
        public string Query { get; set; }
        public IntegrationEngineContext DbContext { get; set; }
        public ILog Log { get; set; }
        public IMailClient MailClient { get; set; }

        public SqlJob()
        {
        }

        public virtual IList<T> RunQuery<T>() 
        {
            try 
            {
                return DbContext.Database.SqlQuery<T>(Query).ToList();
            } 
            catch(ArgumentException exception)
            {
                Log.Error(exception.Message, exception);
            }
            return new List<T>();
        }

        public virtual void Run()
        {
            RunQuery<IDatum>();
        }
    }
}
