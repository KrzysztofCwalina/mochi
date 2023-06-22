// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.FX.AI;
using Azure.FX.Tooling;
using System.Diagnostics;

var t2c = new TextToCode(typeof(Assistant));
t2c.NoMatchFallback = (message) => Assistant.Say(message);
t2c.Logger.Switch.Level = SourceLevels.Information;
t2c.Logger.Listeners.Add(new ConsoleTraceListener());

while (true)
{
    var request = Console.ReadLine();
    if (string.IsNullOrEmpty(request)) continue;

    await t2c.ProcessAsync(request);
    Console.WriteLine();
}

public static class Assistant
{
    private static AIServices s_ai = new AIServices();
    private static List<(string task, string assignedTo)> s_tasks = new List<(string task, string assignedTo)>();

    public static void Say(string message)
    {
        Cli.WriteLine(message, ConsoleColor.Green);
        //s_ai.SpeakAsync(message);
    }

    public static void Add(double x, double y) => Say((x + y).ToString());

    public static void AddTodo(string task, string? assignedTo = default) => s_tasks.Add((task, assignedTo));

    public static void Exit() => Environment.Exit(0);

    public static void TellCurrentTime() => Say($"It's {DateTime.Now.ToString("t")}");

    public static void TellCurrentDate() => Say($"It's {DateTime.Now.ToString("d")}");

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
}

// tell time, tell date
// weather at location
// add numbers, the subtract
// add task to task list, read tasks
// exit app