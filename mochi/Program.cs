﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.FX.Tooling;

var t2c = new TextToCode(typeof(Assistant), (message)=> Cli.WriteLine(message + "\n", ConsoleColor.Green), log:true); 

while (true)
{
    var request = Console.ReadLine();
    if (string.IsNullOrEmpty(request)) continue;

    await t2c.ProcessAsync(request); // executes Code's method that corresponds the closest to the request
}

public static class Assistant
{
    public static void AddNumbers(double x, double y) => Console.WriteLine(x + y);

    public static void AddTask(string task) => Console.WriteLine("TODO: " + task);

    public static void Exit() => Environment.Exit(0);

    //public static void GetWeatherForecast(double longitude, double latitude, DateTimeOffset when) => Console.WriteLine($"it's nice");

    //public static void GetTime(double longitude, double latitude) => Console.WriteLine($"It's {DateTime.Now.ToString("t")}");
}
