// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.FX.AI;
using Azure.FX.Tooling;
using Microsoft.CognitiveServices.Speech;

namespace Mochi;

public static class MochiAssistant
{
    private static AIServices s_ai = new AIServices();
    private static List<(string task, string assignedTo)> s_tasks = new List<(string task, string assignedTo)>();

    public static void Say(string message)
    {
        Cli.WriteLine(message, ConsoleColor.Green);
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

class Mochi
{
    TextToCode _t2c = new TextToCode(typeof(MochiAssistant));
    AIServices _ai;

    public Mochi(AIServices ai)
    {
        _ai = ai;
        _t2c.NoMatchFallback = (message) => MochiAssistant.Say(message);
    }

    public async Task Run()
    {
        var keyword = KeywordRecognitionModel.FromFile("hey_mochi.table");

        while (true)
        {
            Console.WriteLine("waiting ...");

            await _ai.WaitForKeywordAsync(keyword);
            await _ai.StopSpeakingAsync();

            Console.WriteLine("listening ...");
            string recognized = await _ai.RecognizeFromMicrophoneAsync();
            Console.WriteLine(recognized);

            if (string.IsNullOrEmpty(recognized)) continue;

            Console.WriteLine("thinking ...");

            await _t2c.ProcessAsync(recognized);

            Console.WriteLine("speaking ...");
        }
    }
}
