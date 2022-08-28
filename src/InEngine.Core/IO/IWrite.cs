using System;

namespace InEngine.Core.IO;

public interface IConsoleWrite
{
    IConsoleWrite Newline(int count = 1);
    IConsoleWrite Info(object val);
    IConsoleWrite Warning(object val);
    IConsoleWrite Error(object val);
    IConsoleWrite Line(object val);
    IConsoleWrite ColoredLine(object val, ConsoleColor consoleColor);

    IConsoleWrite InfoText(object val);
    IConsoleWrite WarningText(object val);
    IConsoleWrite ErrorText(object val);
    IConsoleWrite Text(object val);
    IConsoleWrite ColoredText(object val, ConsoleColor consoleColor);

    string FlushBuffer();
    void ToFile(string path, string text, bool shouldAppend = false);
}