
var clu = new Clu(typeof(Assistant));
await clu.ExecuteAsync("How to make cookies?");

public static class Assistant
{
    public static void CookieReceipe()
    {
        Console.WriteLine("mix milk and flour");
    }
    public static void GetTime() => Console.WriteLine($"It's {DateTime.Now.ToString("t")}.");
    public static void DontUnderstand() => Console.WriteLine("I don't understand the request");
}
