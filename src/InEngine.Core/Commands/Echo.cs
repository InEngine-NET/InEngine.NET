using System.Threading.Tasks;
using CommandLine;

namespace InEngine.Core.Commands;

/// <summary>
/// Echo some text to the console. Useful for end-to-end testing.
/// </summary>
public class Echo : AbstractCommand
{
    public Echo()
    {
    }

    public Echo(string verbatimText) => VerbatimText = verbatimText;

    [Option("text", HelpText = "The text to echo.")]
    public string VerbatimText { get; init; }

    public override async Task Run() => await Task.Run(() => Line(VerbatimText));
}