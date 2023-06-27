// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure;
using Azure.Data.Tables;
using Azure.FX.AI;
using Azure.FX.Tooling;
using Microsoft.CognitiveServices.Speech;

internal class Program
{
    public static readonly AIServices AI = new AIServices();
    static readonly KeywordRecognitionModel keyword = KeywordRecognitionModel.FromFile("hey_mochi.table");
    static readonly string storageConnectionString = ReadConfigurationSetting("MOCHI_STORAGE_CS");
    private readonly static TableServiceClient tableService = new TableServiceClient(storageConnectionString);
    public readonly static TableClient TasksTable = tableService.GetTableClient("mochitasks");

    private static async Task Main(string[] args)
    {

        TextToCode mochi = new TextToCode(typeof(Mochi));
        mochi.NoMatchFallback = (message) => Mochi.Say(message);

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

    internal static string ReadConfigurationSetting(string settingName)
    {
        var value = Environment.GetEnvironmentVariable(settingName);
        if (value == null)
        {
            var message = $"configuration setting {settingName} not set.";
            throw new Exception(message);
        }
        return value;
    }
}

class ToDo : ITableEntity
{
    public string Task { get; set; }
    public string AssignedTo { get; set; }
    public int Priority { get; set; } = 2;

    public string PartitionKey { get; set; } = "todo";
    public string RowKey { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset? Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public ETag ETag { get; set; }
}

public static class Mochi
{
    public static void Say(string message)
    {
        Cli.WriteLine(message, ConsoleColor.Green);
        Cli.WriteLine("speaking ...");
        Program.AI.SpeakAsync(message);
    }

    public static void AddTask(string task, string assignedTo = default)
    {
        var newTask = new ToDo();
        newTask.Task = task;
        newTask.AssignedTo = assignedTo;
        Program.TasksTable.AddEntity(newTask);
    }

    public static void ListTasks(string assignedTo = default)
    {
        var tasks = Program.TasksTable.Query<ToDo>();

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

    public static void TellCurrentTime() => Say($"It's {DateTime.Now.ToString("t")}");

    public static void TellCurrentDate() => Say($"It's {DateTime.Now.ToString("d")}");

    public static void CurrentWeather(WeatherLocation location)
    {
        (string phrase, int tempF) = Weather.GetCurrent(location);
        Say($"it's {phrase}. {tempF} degrees Fahrenheit");
    }
    public static void CurrentWeather() => CurrentWeather(WeatherLocation.Redmond);
}
