// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.FX.AI;
using mochi.fx;
using System.Diagnostics;

var t2c = new TextToCode(typeof(Assistant));
t2c.NoMatchFallback = (message) => Assistant.Say(message);

//t2c.Logger.Switch.Level = SourceLevels.Information;
//t2c.Logger.Listeners.Add(new ConsoleTraceListener());

while (true)
{
    var request = Console.ReadLine();
    if (string.IsNullOrEmpty(request)) continue;

    await t2c.ProcessAsync(request);
    Console.WriteLine();
}

public static class Assistant
{
    public static void Say(string message) => Cli.WriteLine(message, ConsoleColor.Green);
    public static void Exit() => Environment.Exit(0);

    public static void Add(double x, double y) => Say($"{x+y}");

    public static void TellCurrentTime() => Say($"It's {DateTime.Now:t}");

    public static void TellCurrentDate() => Say($"It's {DateTime.Now:d}");
}