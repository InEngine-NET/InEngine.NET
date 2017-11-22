using System;

namespace InEngine.Core.IO
{
    public class Write : IWrite
    {
        public ConsoleColor InfoColor { get; set; } = ConsoleColor.Green;
        public ConsoleColor WarningColor { get; set; } = ConsoleColor.Yellow;
        public ConsoleColor ErrorColor { get; set; } = ConsoleColor.Red;
        public ConsoleColor LineColor { get; set; } = ConsoleColor.White;

        public IWrite Newline()
        {
            Console.WriteLine();
            return this;
        }

        public IWrite Info(string val)
        {
            return ColoredLine(val, InfoColor);
        }

        public IWrite Error(string val)
        {
            return ColoredLine(val, ErrorColor);
        }

        public IWrite Warning(string val)
        {
            return ColoredLine(val, WarningColor);
        }

        public IWrite Line(string val)
        {
            return ColoredLine(val, LineColor);
        }

        public IWrite ColoredLine(string val, ConsoleColor consoleColor)
        {
            Console.ForegroundColor = consoleColor;
            Console.WriteLine(val);
            Console.ResetColor();
            return this;
        }

        public IWrite InfoText(string val)
        {
            return ColoredText(val, InfoColor);
        }

        public IWrite ErrorText(string val)
        {
            return ColoredText(val, ErrorColor);
        }

        public IWrite WarningText(string val)
        {
            return ColoredText(val, WarningColor);
        }

        public IWrite LineText(string val)
        {
            return ColoredText(val, LineColor);
        }

        public IWrite ColoredText(string val, ConsoleColor consoleColor)
        {
            Console.ForegroundColor = consoleColor;
            Console.Write(val);
            Console.ResetColor();
            return this;
        }
    }
}
