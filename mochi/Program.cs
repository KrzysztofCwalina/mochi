// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.FX.AI;
using Azure.FX.Tooling;

var t2c = new TextToCode(typeof(Assistant));
t2c.NoMatchFallback = (message) => Cli.WriteLine(message, ConsoleColor.Green);
t2c.Log = true;

while (true)
{
    var request = Console.ReadLine();
    if (string.IsNullOrEmpty(request)) continue;

    await t2c.ProcessAsync(request); // executes Code's method that corresponds the closest to the request
    Console.WriteLine();
}

public static class Assistant
{
    public static void Add(double x, double y) => Console.WriteLine(x + y);

    public static void AddTask(string task) => Console.WriteLine("ToDo added: " + task);

    public static void Exit() => Environment.Exit(0);

    public static void Time() => Console.WriteLine($"It's {DateTime.Now.ToString("t")}");
    public static void Time(double longitude, double latitude) => Console.WriteLine($"It's {DateTime.Now.ToString("t")}");

    public static void WeatherForecast(DateTimeOffset when, double longitude, double latitude) => Console.WriteLine($"it will be rainy!");

    public static void CurrentWeather(double longitude, double latitude) => Console.WriteLine($"it's sunny there!");

    public static void CurrentWeather() => Console.WriteLine($"it's sunny!");
}
