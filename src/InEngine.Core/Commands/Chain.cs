using System;
using System.Collections.Generic;
using System.Linq;
using InEngine.Core.Exceptions;
using Quartz;

namespace InEngine.Core.Commands
{
    public class Chain : AbstractCommand
    {
        public IList<AbstractCommand> Commands { get; set; } = new List<AbstractCommand>();

        public override void Run()
        {
            Commands.ToList().ForEach(x => { 
                try
                {
                    x.Run();
                }
                catch (Exception exception)
                {
                    throw new CommandChainFailedException(x.Name, exception);
                }
            });
        }

        public override void Execute(IJobExecutionContext context)
        {
            var properties = GetType().GetProperties();
            context.MergedJobDataMap.ToList().ForEach(x => {
                var property = properties.FirstOrDefault(y => y.Name == x.Key);
                if (property != null)
                    property.SetValue(this, x.Value);
            });

            try
            {
                ExecutionLifeCycle.FirePreActions(this);
                Run();
                ExecutionLifeCycle.FirePostActions(this);
            }
            catch (Exception exception)
            {
                Failed(exception);
            }
        }
    }
}
