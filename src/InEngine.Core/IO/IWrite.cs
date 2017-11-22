using System;

namespace InEngine.Core.IO
{
    public interface IWrite
    {
        void Info(string val);
        void Warning(string val);
        void Error(string val);
        void Text(string val);
        void Line(string val, ConsoleColor consoleColor = ConsoleColor.White);
    }
}
