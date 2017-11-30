using System;

namespace InEngine.Core.IO
{
    public interface IWrite
    {
        IWrite Newline(int count = 1);
        IWrite Info(string val);
        IWrite Warning(string val);
        IWrite Error(string val);
        IWrite Line(string val);
        IWrite ColoredLine(string val, ConsoleColor consoleColor);

        IWrite InfoText(string val);
        IWrite WarningText(string val);
        IWrite ErrorText(string val);
        IWrite Text(string val);
        IWrite ColoredText(string val, ConsoleColor consoleColor);

        string FlushBuffer();
        void ToFile(string path, string text, bool shouldAppend = false);
    }
}
