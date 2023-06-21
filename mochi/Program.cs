
var t2c = new TextToCode(typeof(Code)); // extracts "schema" from the Code class

while (true)
{
    var request = Console.ReadLine();
    await t2c.ExecuteAsync(request); // executes Code's method that corresponds the closest to the request
}

public static class Code
{
    public static void GetWeatherForecast(DateTimeOffset when) => Console.WriteLine($"it's nice");

    public static void GetTime(string location = default) => Console.WriteLine($"It's {DateTime.Now.ToString("t")}");
    public static void DontUnderstand() => Console.WriteLine("I don't understand the request");

    public static void Add(double x, double y) => Console.WriteLine(x + y);
}
