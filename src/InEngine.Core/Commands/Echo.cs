using CommandLine;

namespace InEngine.Core.Commands
{
    /// <summary>
    /// Echo some text to the console. Useful for end-to-end testing.
    /// </summary>
    public class Echo : AbstractCommand
    {
        public Echo()
        {
        }

        public Echo(string verbatimText)
        {
            VerbatimText = verbatimText;
        }

        [Option("text", HelpText = "The text to echo.")]
        public string VerbatimText { get; set; }

        public override void Run()
        {
            Line(VerbatimText);
        }
    }
}