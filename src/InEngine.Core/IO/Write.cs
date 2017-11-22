using System;

namespace InEngine.Core.IO
{
    public class Write
    {
        public ConsoleColor InfoColor { get; set; } = ConsoleColor.Green;
        public ConsoleColor WarningColor { get; set; } = ConsoleColor.Yellow;
        public ConsoleColor ErrorColor { get; set; } = ConsoleColor.Red;
        public ConsoleColor LineColor { get; set; } = ConsoleColor.White;

        public Write Newline()
        {
            Console.WriteLine();
            return this;
        }

        public Write Info(string val)
        {
            ColoredLine(val, InfoColor);
            return this;
        }

        public Write Error(string val)
        {
            ColoredLine(val, ErrorColor);
            return this;
        }

        public Write Warning(string val)
        {
            ColoredLine(val, WarningColor);
            return this;
        }

        public Write Line(string val)
        {
            ColoredLine(val, LineColor);
            return this;
        }

        public static void ColoredLine(string val, ConsoleColor consoleColor)
        {
            Console.ForegroundColor = consoleColor;
            Console.WriteLine(val);
            Console.ResetColor();
        }


        public Write InfoText(string val)
        {
            ColoredText(val, InfoColor);
            return this;
        }

        public Write ErrorText(string val)
        {
            ColoredText(val, ErrorColor);
            return this;
        }

        public Write WarningText(string val)
        {
            ColoredText(val, WarningColor);
            return this;
        }

        public Write LineText(string val)
        {
            ColoredText(val, LineColor);
            return this;
        }

        public static void ColoredText(string val, ConsoleColor consoleColor)
        {
            Console.ForegroundColor = consoleColor;
            Console.Write(val);
            Console.ResetColor();
        }
    }
}
