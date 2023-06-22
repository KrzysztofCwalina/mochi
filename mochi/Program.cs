// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.FX.AI;
using Azure.FX.Tooling;
using Microsoft.CognitiveServices.Speech;

var ai = new AIServices();
var keyword = KeywordRecognitionModel.FromFile("hey_mochi.table");

TextToCode mochi = new TextToCode(typeof(Mochi));
mochi.NoMatchFallback = (message) => Mochi.Say(message);

while (true)
{
    Cli.WriteLine("waiting ...", ConsoleColor.Blue);

    await ai.WaitForKeywordAsync(keyword);
    await ai.StopSpeakingAsync();

    Cli.WriteLine("listening ...", ConsoleColor.Blue);
    string recognizedSpeech = await ai.RecognizeFromMicrophoneAsync();
    if (string.IsNullOrEmpty(recognizedSpeech)) continue;
    Cli.WriteLine(recognizedSpeech, ConsoleColor.Blue);

    Cli.WriteLine("thinking ...", ConsoleColor.Blue);
    await mochi.ProcessAsync(recognizedSpeech);
}

public static class Mochi
{
    private static AIServices s_ai = new AIServices();
    private static List<(string task, string assignedTo)> s_tasks = new List<(string task, string assignedTo)>();

    public static void Say(string message)
    {
        Cli.WriteLine(message, ConsoleColor.Green);
        Cli.WriteLine("speaking ...");
        s_ai.SpeakAsync(message);
    }

    public static void AddTask(string task, string assignedTo = default) => s_tasks.Add((task, assignedTo));

    public static void ListTasks(string assignedTo = default)
    {
        foreach (var task in s_tasks)
        {
            if (string.IsNullOrEmpty(assignedTo) || assignedTo.Equals(task.assignedTo))
            {
                var taskMessage = task.task;
                if (string.IsNullOrEmpty(assignedTo) && !string.IsNullOrEmpty(task.assignedTo))
                    taskMessage += ". assigned to " + task.assignedTo.ToString();

                Say(taskMessage);
            }
        }
    }

    public static void TellCurrentTime() => Say($"It's {DateTime.Now.ToString("t")}");

    public static void TellCurrentDate() => Say($"It's {DateTime.Now.ToString("d")}");
}