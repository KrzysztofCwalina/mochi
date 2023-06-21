// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

var t2c = new TextToCode(typeof(Code)); // extracts "schema" from the Code class

while (true)
{
    var request = Console.ReadLine();
    if (string.IsNullOrEmpty(request)) continue;

    await t2c.ExecuteAsync(request); // executes Code's method that corresponds the closest to the request
}

public static class Code
{
    public static void GetWeatherForecast(double longitude, double latitude, DateTimeOffset when) => Console.WriteLine($"it's nice");

    public static void GetTime(double longitude, double latitude) => Console.WriteLine($"It's {DateTime.Now.ToString("t")}");
    public static void DontUnderstand() => Console.WriteLine("I don't understand the request");

    public static void CommunicateWithUser(string textToSpeak) => Console.WriteLine(textToSpeak);

    public static void Add(double x, double y) => Console.WriteLine(x + y);
}
