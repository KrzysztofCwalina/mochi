// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using mochi;
using NUnit.Framework;

namespace mochi.tests;

public class ToDoTests
{
    static SettingsClient settings = new SettingsClient(new Uri("https://cme4194165e0f246c.vault.azure.net/"));
    ToDoClient client = new ToDoClient(settings);

    [Test]
    public void Basics()
    {
        client.Add(new ToDo() { AssignedTo = "Krzysztof", Task = "buy milk" });
    }
}