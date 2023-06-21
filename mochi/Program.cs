// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

var t2c = new TextToCode(typeof(Assistant)); // extracts "schema" from the Code class

while (true)
{
    var request = Console.ReadLine();
    if (string.IsNullOrEmpty(request)) continue;

    await t2c.ExecuteAsync(request); // executes Code's method that corresponds the closest to the request
}

public static class Assistant
{
    public static void Default(string text) => Console.WriteLine(text);

    public static void Add(double x, double y) => Console.WriteLine(x + y);

    //public static void GetWeatherForecast(double longitude, double latitude, DateTimeOffset when) => Console.WriteLine($"it's nice");

    //public static void GetTime(double longitude, double latitude) => Console.WriteLine($"It's {DateTime.Now.ToString("t")}");
}
