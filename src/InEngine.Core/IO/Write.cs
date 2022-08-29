using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace InEngine.Core.IO;

public class Write : IConsoleWrite
{
    private static readonly Mutex ConsoleOutputLock = new();
    public static readonly Mutex FileOutputLock = new();

    public ConsoleColor InfoColor { get; set; } = ConsoleColor.Green;
    public ConsoleColor WarningColor { get; set; } = ConsoleColor.Yellow;
    public ConsoleColor ErrorColor { get; set; } = ConsoleColor.Red;
    public ConsoleColor LineColor { get; set; } = ConsoleColor.White;
    public List<string> Buffer { get; set; } = new();
    public bool IsBufferEnabled { get; set; }

    public Write() : this(true)
    {
    }

    public Write(bool isBufferEnabled) => IsBufferEnabled = isBufferEnabled;

    #region Sync Methods

    public IConsoleWrite Newline(int count = 1)
    {
        for (var i = 0; i < count; i++)
            Console.WriteLine();
        return this;
    }

    public IConsoleWrite Info(object val) => LineWithColor(val, InfoColor);
    public IConsoleWrite Error(object val) => LineWithColor(val, ErrorColor);
    public IConsoleWrite Warning(object val) => LineWithColor(val, WarningColor);
    public IConsoleWrite Line(object val) => LineWithColor(val, LineColor);

    public IConsoleWrite LineWithColor(object val, ConsoleColor consoleColor)
    {
        TextWithColor(val, consoleColor, true);
        return this;
    }

    public IConsoleWrite InfoText(object val) => TextWithColor(val, InfoColor);
    public IConsoleWrite ErrorText(object val) => TextWithColor(val, ErrorColor);
    public IConsoleWrite WarningText(object val) => TextWithColor(val, WarningColor);
    public IConsoleWrite Text(object val) => TextWithColor(val, LineColor);

    public IConsoleWrite TextWithColor(object val, ConsoleColor consoleColor, bool writeLine = false)
    {
        var text = BeginWriting(val, consoleColor);

        if (writeLine)
            Console.WriteLine(val);
        else
            Console.Write(val);

        EndWriting(text, writeLine);
        return this;
    }

    #endregion

    #region Async Methods

    public async Task NewlineAsync(int count = 1)
    {
        for (var i = 0; i < count; i++)
            await Console.Out.WriteLineAsync();
    }

    public async Task InfoAsync(object val) => await LineWithColorAsync(val, InfoColor);
    public async Task ErrorAsync(object val) => await LineWithColorAsync(val, ErrorColor);
    public async Task WarningAsync(object val) => await LineWithColorAsync(val, WarningColor);
    public async Task LineAsync(object val) => await LineWithColorAsync(val, LineColor);

    public async Task LineWithColorAsync(object val, ConsoleColor consoleColor) =>
        await TextWithColorAsync(val, consoleColor, true);

    public async Task InfoTextAsync(object val) => await TextWithColorAsync(val, InfoColor);
    public async Task ErrorTextAsync(object val) => await TextWithColorAsync(val, ErrorColor);
    public async Task WarningTextAsync(object val) => await TextWithColorAsync(val, WarningColor);
    public async Task TextAsync(object val) => await TextWithColorAsync(val, LineColor);

    public async Task TextWithColorAsync(object val, ConsoleColor consoleColor, bool writeLine = false)
    {
        var text = BeginWriting(val, consoleColor);

        if (writeLine)
            await Console.Out.WriteLineAsync(text);
        else
            await Console.Out.WriteAsync(text);

        EndWriting(text, writeLine);
    }

    #endregion

    protected string BeginWriting(object val, ConsoleColor consoleColor)
    {
        ConsoleOutputLock.WaitOne();
        Console.ForegroundColor = consoleColor;
        return val?.ToString() ?? string.Empty;
    }

    protected void EndWriting(string text, bool writeLine)
    {
        Console.ResetColor();
        if (IsBufferEnabled)
        {
            Buffer.Add(text);
            if (writeLine)
                Buffer.Add(Environment.NewLine);
        }

        ConsoleOutputLock.ReleaseMutex();
    }

    public string FlushBuffer()
    {
        var str = string.Join("\n", Buffer);
        Buffer.Clear();
        return str;
    }

    public void ToFile(string path, string text, bool shouldAppend = false)
    {
        FileOutputLock.WaitOne();
        if (shouldAppend)
            File.AppendAllText(path, text);
        else
            File.WriteAllText(path, text);

        FileOutputLock.ReleaseMutex();
    }
}