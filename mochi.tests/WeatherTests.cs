// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using mochi;
using NUnit.Framework;
using System.Diagnostics;

namespace mochi.tests;

public class WeatherTests
{
    static SettingsClient settings = new SettingsClient(new Uri("https://cme4194165e0f246c.vault.azure.net/"));
    WeatherClient client = new WeatherClient(settings);

    [Test]
    public void Basics()
    {
        var weather = client.GetCurrent(WeatherLocation.Redmond);
        Debug.WriteLine(weather.Description);
        Debug.WriteLine(weather.TempF);
    }
}