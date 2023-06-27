// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using mochi.fx;
using NUnit.Framework;

namespace mochi.tests;

public class SettingsTests
{
    static SettingsClient settings = new SettingsClient(new Uri("https://cme4194165e0f246c.vault.azure.net/"));

    [Test]
    public void Basics()
    {
        var secret = settings.GetSecret("test-secret");
        Assert.That(secret, Is.EqualTo("nobody knows"));
    }
}