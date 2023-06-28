// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.FX.AI;
using mochi;
using Microsoft.CognitiveServices.Speech;
using System.Diagnostics;

internal class Program
{
    static readonly KeywordRecognitionModel keyword = KeywordRecognitionModel.FromFile("hey_mochi.table");

    internal static readonly SettingsClient Settings = new SettingsClient(new Uri("https://cme4194165e0f246c.vault.azure.net/"));
    internal static readonly AIServices AI = new AIServices(Settings);

    private static async Task Main(string[] args)
    {
        TextToCode mochi = new TextToCode(typeof(Mochi));
        mochi.NoMatchFallback = (message) => Mochi.Say(message);
        mochi.Logger.Switch.Level = SourceLevels.Information;
        mochi.Logger.Listeners.Add(new ConsoleTraceListener());

        while (true)
        {
            Cli.WriteLine("waiting ...", ConsoleColor.Blue);

            await AI.WaitForKeywordAsync(keyword);
            await AI.StopSpeakingAsync();

            Cli.WriteLine("listening ...", ConsoleColor.Blue);

            //string recognizedSpeech = Console.ReadLine();
            string recognizedSpeech = await AI.RecognizeFromMicrophoneAsync();
            if (string.IsNullOrEmpty(recognizedSpeech)) continue;
            Cli.WriteLine(recognizedSpeech, ConsoleColor.Blue);

            Cli.WriteLine("thinking ...", ConsoleColor.Blue);
            await mochi.ProcessAsync(recognizedSpeech);
        }
    }
}

public static class Mochi
{
    static readonly WeatherClient s_weather = new WeatherClient(Program.Settings);
    static readonly ToDoClient s_todos = new ToDoClient(Program.Settings);
    static readonly EMailClient s_email = new EMailClient(Program.Settings);

    public static void Say(string message)
    {
        Cli.WriteLine(message, ConsoleColor.Green);
        Cli.WriteLine("speaking ...");
        Program.AI.SpeakAsync(message, blocking: false);
    }

    public static void AddTask(string task, string assignedTo = default)
    {
        var newTask = new ToDo();
        newTask.Task = task;
        newTask.AssignedTo = assignedTo;
        s_todos.Add(newTask);
    }

    public static void ListTasks(string assignedTo = default)
    {
        var tasks = s_todos.Get(assignedTo);

        foreach (var task in tasks)
        {
            if (string.IsNullOrEmpty(assignedTo) || assignedTo.Equals(task.AssignedTo))
            {
                var taskMessage = task.Task;
                if (string.IsNullOrEmpty(assignedTo) && !string.IsNullOrEmpty(task.AssignedTo))
                    taskMessage += ". assigned to " + task.AssignedTo;

                Say(taskMessage);
            }
        }
    }

    public static void SendEMail(EmailReceipient to, string message, string subject = "from Mochi")
    {
        var receipeint = Mappings.EMailAddresses[to];
        Program.AI.SpeakAsync($"Sending: {message} to {receipeint}. Do you want me to proceed?", blocking: true);
        string response = Program.AI.RecognizeFromMicrophoneAsync().GetAwaiter().GetResult();
        if (response.StartsWith("yes", StringComparison.OrdinalIgnoreCase) ||
            response.StartsWith("yup", StringComparison.OrdinalIgnoreCase) ||
            response.StartsWith("certainly", StringComparison.OrdinalIgnoreCase) ||
            response.StartsWith("go ahead", StringComparison.OrdinalIgnoreCase) ||
            response.StartsWith("proceed", StringComparison.OrdinalIgnoreCase)
        )
        {
            s_email.Send(receipeint, subject, message);
            Say("Sent.");
        }
        else
            Say("Cancelled.");
    }
    public static void TellCurrentTime() => Say($"It's {DateTime.Now:t}");

    public static void TellCurrentDate() => Say($"It's {DateTime.Now:d}");

    public static void CurrentWeather(WeatherLocation location)
    {
        (double Latitude, double Longitude) coordinates = Mappings.Coordinates[location];

        var weather = s_weather.GetCurrent(coordinates.Latitude,coordinates.Longitude);
        Say($"it's {weather.Description}. {weather.TempF} degrees Fahrenheit");
    }
    public static void CurrentWeather() => CurrentWeather(WeatherLocation.Redmond);
}

internal static class Mappings
{
    internal static readonly Dictionary<WeatherLocation, ValueTuple<double, double>> Coordinates = new Dictionary<WeatherLocation, ValueTuple<double, double>>(
        new KeyValuePair<WeatherLocation, ValueTuple<double, double>>[] {
            new KeyValuePair<WeatherLocation, ValueTuple<double, double>>(WeatherLocation.Redmond, (47.67, -122.12)),
            new KeyValuePair<WeatherLocation, ValueTuple<double, double>>(WeatherLocation.LaQuinta, (33.66, -116.30)),
            new KeyValuePair<WeatherLocation, ValueTuple<double, double>>(WeatherLocation.Warsaw, (52.22, 21.01)),
            new KeyValuePair<WeatherLocation, ValueTuple<double, double>>(WeatherLocation.Pulawy, (51.25, 21.58)),
            new KeyValuePair<WeatherLocation, ValueTuple<double, double>>(WeatherLocation.Seoul, (37.53, 127.02)),
            new KeyValuePair<WeatherLocation, ValueTuple<double, double>>(WeatherLocation.LosAngeles, (34.05, -118.24)),
            new KeyValuePair<WeatherLocation, ValueTuple<double, double>>(WeatherLocation.SanLouisObispo, (35.27, -120.68)),
        }
    );

    internal static readonly Dictionary<EmailReceipient, string> EMailAddresses = new Dictionary<EmailReceipient, string>(
        new KeyValuePair<EmailReceipient, string>[] {
            new KeyValuePair<EmailReceipient, string>(EmailReceipient.Krzysztof, "kcwalina@outlook.com"),
            new KeyValuePair<EmailReceipient, string>(EmailReceipient.Ela, "ecwalina@outlook.com"),
            new KeyValuePair<EmailReceipient, string>(EmailReceipient.Olenka, "ocwalina@outlook.com"),
            new KeyValuePair<EmailReceipient, string>(EmailReceipient.Alex, "ocwalina@outlook.com"),
            new KeyValuePair<EmailReceipient, string>(EmailReceipient.Alan, "acwalina@outlook.com"),
            new KeyValuePair<EmailReceipient, string>(EmailReceipient.Busio, "acwalina@outlook.com"),
        }
    );
}
public enum WeatherLocation
{
    Redmond,        // "47.67, -122.12"
    LaQuinta,       // "33.66, -116.30"
    Warsaw,         // "52.22, 21.01"
    Pulawy,         // "51.25, 21.58"
    Seoul,          // "37.53, 127.02"
    LosAngeles,     // "34.05, -118.24"
    SanLouisObispo  // "35.27, -120.68"
}

public enum EmailReceipient
{
    Krzysztof,
    Ela,
    Olenka,
    Alex,
    Alan,
    Busio
}
