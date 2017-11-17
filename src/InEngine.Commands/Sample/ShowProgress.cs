using InEngine.Core;

namespace InEngine.Commands.Sample
{
    /*
     * The AbstractCommand class adds functionality, including a logger and a 
     * progress bar.
     */
    public class ShowProgress : AbstractCommand
    {
        /*
         * Note that the override keyword is necessary in the Run method 
         * signature as the base class method is virtual.
         */
        public override CommandResult Run()
        {
            // Define the ticks (aka steps) for the command...
            var maxTicks = 100000;
            SetProgressBarMaxTicks(maxTicks);

            // Do some work...
            for (var i = 0; i <= maxTicks;i++)
            {
                // Update the command's progress
                UpdateProgress(i);
            }

            return new CommandResult(true);
        }
    }
}
