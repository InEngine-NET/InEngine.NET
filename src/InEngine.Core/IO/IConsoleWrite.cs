using System;

namespace InEngine.Core.IO;

using System.Threading.Tasks;

public interface IConsoleWrite
{
    #region Sync Methods

    IConsoleWrite Newline(int count = 1);
    IConsoleWrite Info(object val);
    IConsoleWrite Warning(object val);
    IConsoleWrite Error(object val);
    IConsoleWrite Line(object val);
    IConsoleWrite LineWithColor(object val, ConsoleColor consoleColor);

    IConsoleWrite InfoText(object val);
    IConsoleWrite WarningText(object val);
    IConsoleWrite ErrorText(object val);
    IConsoleWrite Text(object val);
    IConsoleWrite TextWithColor(object val, ConsoleColor consoleColor, bool writeLine = false);

    #endregion

    #region Async Methods

    Task NewlineAsync(int count = 1);
    Task InfoAsync(object val);
    Task WarningAsync(object val);
    Task ErrorAsync(object val);
    Task LineAsync(object val);
    Task LineWithColorAsync(object val, ConsoleColor consoleColor);

    Task InfoTextAsync(object val);
    Task WarningTextAsync(object val);
    Task ErrorTextAsync(object val);
    Task TextAsync(object val);
    Task TextWithColorAsync(object val, ConsoleColor consoleColor, bool writeLine = false);

    #endregion

    string FlushBuffer();
    void ToFile(string path, string text, bool shouldAppend = false);
}