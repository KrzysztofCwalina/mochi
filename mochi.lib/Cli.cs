// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace mochi;

public static class Cli
{
    public static bool IsDiagnosticsTraceEnabled = false;
    public static void WriteError(string line)
        => Cli.WriteLine(line, ConsoleColor.Red);

    public static void WriteWarning(string line)
        => Cli.WriteLine(line, ConsoleColor.Yellow);

    public static void WritePrompt(string line)
        => Cli.Write(line, ConsoleColor.Blue);

    public static void WriteSuccess(string line)
        => Cli.WriteLine(line, ConsoleColor.Green);

    public static void WriteDiagnosticsTrace(string line)
    {
        if (IsDiagnosticsTraceEnabled) Cli.WriteLine(line, ConsoleColor.Gray);
    }
    public static void WriteLine(string line, ConsoleColor color)
    {
        var previous = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = color;
            Console.WriteLine(line);
        }
        finally
        {
            Console.ForegroundColor = previous;
        }
    }
    public static void Write(string line, ConsoleColor color)
    {
        var previous = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = color;
            Console.Write(line);
        }
        finally
        {
            Console.ForegroundColor = previous;
        }
    }
    public static void WriteLine(string line) => Console.WriteLine(line);
    public static void WriteLine() => Console.WriteLine();

    public static IDisposable Spin() => CliSpinner.Start();
}

internal class CliSpinner : IDisposable
{
    volatile bool _mainDone = false;
    volatile bool _spinDone = false;
    char[] seq = new char[] { '|', '/', '-', '\\', '|', '/', '-', '\\' };
    int current = 0;
    char Next() => seq[current++ % seq.Length];

    int _left;
    int _top;

    public static CliSpinner Start()
    {
        var spinner = new CliSpinner();
        spinner.Spin();
        return spinner;
    }

    public void Spin()
    {
        Task.Run(() =>
        {
            Console.CursorVisible = false;
            _left = Console.CursorLeft;
            _top = Console.CursorTop;
            while (!_mainDone)
            {
                Thread.Sleep(200);
                Print(Next());
            }
            Console.CursorVisible = true;
            _spinDone = true;
        });
    }

    private void Print(char c)
    {
        var left = Console.CursorLeft;
        var top = Console.CursorTop;
        Console.CursorLeft = _left;
        Console.CursorTop = _top;
        Console.Write(c);
        Console.CursorLeft = left;
        Console.CursorTop = top;
    }

    public void Dispose()
    {
        _mainDone = true;
        while (!_spinDone) Thread.Sleep(50);
        Print(' ');
        Console.CursorLeft = _left;
        Console.CursorTop = _top;
    }
}
