// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure;
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
    private static List<ToDo> s_tasks = new List<ToDo>();

    public static void Say(string message)
    {
        Cli.WriteLine(message, ConsoleColor.Green);
        //s_ai.SpeakAsync(message);
    }

    public static void Add(double x, double y) => Say((x + y).ToString());

    public static void AddTodo(string task, string? assignedTo = default, TodoPriority priority = TodoPriority.Default) 
        => s_tasks.Add(new ToDo() { Task = task, AssignedTo = assignedTo, Priority = priority});

    public static void Exit() => Environment.Exit(0);

    public static void TellCurrentTime() => Say($"It's {DateTime.Now.ToString("t")}");

    public static void TellCurrentDate() => Say($"It's {DateTime.Now.ToString("d")}");

    public static void ListTasks(string? assignedTo = default)
    {
        foreach (var task in s_tasks)
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
}

public enum TodoPriority
{
    Default = 0,
    Low = 1,
    High = 2
}

public struct ToDo
{
    public string Task { get; set; }
    public string? AssignedTo { get; set; }
    public TodoPriority Priority { get; set; } 
}

// tell time, tell date
// weather at location
// add numbers, the subtract
// add task to task list, read tasks
// exit app