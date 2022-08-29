using CommandLine;

namespace InEngine.Core.Commands;

public class CommandPlugin : AbstractPlugin
{
    [VerbOption("echo", HelpText = "Echo some text to the console. Useful for end-to-end testing.")]
    public Echo Echo { get; set; }

    [VerbOption("exec", HelpText = "Execute an external program.")]
    public Exec Exec { get; set; }
    
    [VerbOption("sleep", HelpText = "Sleep (in seconds)")]
    public Sleep Sleep { get; set; }
}