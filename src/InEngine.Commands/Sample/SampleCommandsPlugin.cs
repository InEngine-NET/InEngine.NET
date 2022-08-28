using CommandLine;
using InEngine.Core;

namespace InEngine.Commands.Sample;

public class SampleCommandsPlugin : AbstractPlugin
{
    [VerbOption("sample:pause", HelpText = "Pause for a few seconds.")]
    public Pause Pause { get; set; }

    [VerbOption("sample:show-progress", HelpText = "A sample command to demonstrate the progress bar.")]
    public ShowProgress ShowProgress { get; set; }

    [VerbOption("sample:say-hello", HelpText = "A sample command to say \"hello\".")]
    public SayHello SayHello { get; set; }

    [VerbOption("sample:minimal", HelpText = "A minimal implementation of a command - does nothing.")]
    public Minimal Minimal { get; set; }
}