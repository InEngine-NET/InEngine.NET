using System;

namespace InEngine.Core.IO;

public interface IWrite
{
    IWrite Newline(int count = 1);
    IWrite Info(object val);
    IWrite Warning(object val);
    IWrite Error(object val);
    IWrite Line(object val);
    IWrite ColoredLine(object val, ConsoleColor consoleColor);

    IWrite InfoText(object val);
    IWrite WarningText(object val);
    IWrite ErrorText(object val);
    IWrite Text(object val);
    IWrite ColoredText(object val, ConsoleColor consoleColor);

    string FlushBuffer();
    void ToFile(string path, string text, bool shouldAppend = false);
}