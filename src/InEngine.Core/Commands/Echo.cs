using System;
using CommandLine;

namespace InEngine.Core.Commands
{
    /// <summary>
    /// Echo some text to the console. Useful for end-to-end testing.
    /// </summary>
    public class Echo : AbstractCommand
    {
        [Option("text", HelpText = "The text to echo.")]
        public string Text { get; set; }

        public override void Run()
        {
            Write.Line(Text);
        }
    }
}
