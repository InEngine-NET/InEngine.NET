using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace InEngine.Core.IO
{
    public class Write : IWrite
    {
        static Mutex consoleOutputLock = new Mutex();
        static Mutex fileOutputLock = new Mutex();

        public ConsoleColor InfoColor { get; set; } = ConsoleColor.Green;
        public ConsoleColor WarningColor { get; set; } = ConsoleColor.Yellow;
        public ConsoleColor ErrorColor { get; set; } = ConsoleColor.Red;
        public ConsoleColor LineColor { get; set; } = ConsoleColor.White;
        public List<string> Buffer { get; set; } = new List<string>();

        public IWrite Newline(int count = 1)
        {
            for (var i = 0; i < count; i++)
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
            consoleOutputLock.WaitOne();
            Console.ForegroundColor = consoleColor;
            Console.WriteLine(val);
            Console.ResetColor();
            consoleOutputLock.ReleaseMutex();
            Buffer.Add(val);
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

        public IWrite Text(string val)
        {
            return ColoredText(val, LineColor);
        }

        public IWrite ColoredText(string val, ConsoleColor consoleColor)
        {
            consoleOutputLock.WaitOne();
            Console.ForegroundColor = consoleColor;
            Console.Write(val);
            Console.ResetColor();
            consoleOutputLock.ReleaseMutex();
            Buffer.Add(val);
            return this;
        }

        public string FlushBuffer()
        {
            var str = string.Join("\n", Buffer);
            Buffer.Clear();
            return str;
        }

        public void ToFile(string path, string text, bool shouldAppend = false)
        {
            fileOutputLock.WaitOne();
            if (!File.Exists(path))
                File.Create(path);
            if (shouldAppend)
                File.AppendAllText(path, text);
            else
                File.WriteAllText(path, text);
            fileOutputLock.ReleaseMutex();
        }
    }
}
