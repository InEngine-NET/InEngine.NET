using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace InEngine.Core.IO;

public class Write : IConsoleWrite
{
    static readonly Mutex consoleOutputLock = new Mutex();
    static readonly Mutex fileOutputLock = new Mutex();

    public ConsoleColor InfoColor { get; set; } = ConsoleColor.Green;
    public ConsoleColor WarningColor { get; set; } = ConsoleColor.Yellow;
    public ConsoleColor ErrorColor { get; set; } = ConsoleColor.Red;
    public ConsoleColor LineColor { get; set; } = ConsoleColor.White;
    public List<string> Buffer { get; set; } = new List<string>();
    public bool IsBufferEnabled { get; set; }

    public Write() : this(true)
    {
    }

    public Write(bool isBufferEnabled)
    {
        IsBufferEnabled = isBufferEnabled;
    }

    public IConsoleWrite Newline(int count = 1)
    {
        for (var i = 0; i < count; i++)
            Console.WriteLine();
        return this;
    }

    public IConsoleWrite Info(object val)
    {
        return ColoredLine(val, InfoColor);
    }

    public IConsoleWrite Error(object val)
    {
        return ColoredLine(val, ErrorColor);
    }

    public IConsoleWrite Warning(object val)
    {
        return ColoredLine(val, WarningColor);
    }

    public IConsoleWrite Line(object val)
    {
        return ColoredLine(val, LineColor);
    }

    public IConsoleWrite ColoredLine(object val, ConsoleColor consoleColor)
    {
        WriteColoredLineOrText(val, consoleColor, true);
        return this;
    }

    public IConsoleWrite InfoText(object val)
    {
        return ColoredText(val, InfoColor);
    }

    public IConsoleWrite ErrorText(object val)
    {
        return ColoredText(val, ErrorColor);
    }

    public IConsoleWrite WarningText(object val)
    {
        return ColoredText(val, WarningColor);
    }

    public IConsoleWrite Text(object val)
    {
        return ColoredText(val, LineColor);
    }

    public IConsoleWrite ColoredText(object val, ConsoleColor consoleColor)
    {
        WriteColoredLineOrText(val, consoleColor, false);
        return this;
    }

    void WriteColoredLineOrText(object val, ConsoleColor consoleColor, bool writeLine)
    {
        if (val == null)
            val = String.Empty;
        consoleOutputLock.WaitOne();
        Console.ForegroundColor = consoleColor;
        if (writeLine)
            Console.WriteLine(val);
        else
            Console.Write(val);
        Console.ResetColor();
        if (IsBufferEnabled)
        {
            Buffer.Add(val.ToString());
            if (writeLine)
                Buffer.Add(Environment.NewLine);
        }

        consoleOutputLock.ReleaseMutex();
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
        if (shouldAppend)
            File.AppendAllText(path, text);
        else
            File.WriteAllText(path, text);

        fileOutputLock.ReleaseMutex();
    }
}