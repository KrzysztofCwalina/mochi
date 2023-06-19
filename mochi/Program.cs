
var t2c = new TextToCode(typeof(Code));

while (true)
{
    var request = Console.ReadLine();
    await t2c.ExecuteAsync(request);
}

public static class Code
{
    public static void GetTime() => Console.WriteLine($"It's {DateTime.Now.ToString("t")}");
    public static void DontUnderstand() => Console.WriteLine("I don't understand the request");
}
