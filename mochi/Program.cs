
var t2c = new TextToCode(typeof(Code)); // extracts "schema" from the Code class

while (true)
{
    var request = Console.ReadLine();
    await t2c.ExecuteAsync(request); // executes Code's method that corresponds the closest to the request
}

public static class Code
{
    public static void GetTime() => Console.WriteLine($"It's {DateTime.Now.ToString("t")}");
    public static void DontUnderstand() => Console.WriteLine("I don't understand the request");
}
