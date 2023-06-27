// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure;

public class Weather
{
    static readonly HttpClient client = new HttpClient();

    public static (string phrase, int tempF) GetCurrent(WeatherLocation location)
    {
        // https://learn.microsoft.com/en-us/rest/api/maps/weather/get-daily-forecast?tabs=HTTP

        var sharedKey = Program.ReadConfigurationSetting("MOCHI_MAPS_SHAREDKEY");

        if (!coor.TryGetValue(location, out var coordinates)){
            coordinates = "47.67, -122.12"; // Redmond, WA
        }
        var uri = $"https://atlas.microsoft.com/weather/currentConditions/json?api-version=1.1&subscription-key={sharedKey}&query={coordinates}&unit=imperial";
        var message = new HttpRequestMessage(HttpMethod.Get, uri);
        message.Headers.Add("x-ms-client-id", "8e426f23-f50b-4eb7-ae88-0b33da5904d3");
        var response = client.Send(message);
        var data = BinaryData.FromStream(response.Content.ReadAsStream());
        var json = data.ToDynamicFromJson(Azure.Core.Serialization.JsonPropertyNames.CamelCase);
        var weather = json.results[0];
        var phrase = weather.Phrase;
        var tempF = (int)(double)weather.Temperature.Value;
        return (phrase, tempF);
    }

    static readonly Dictionary<WeatherLocation, string> coor = new Dictionary<WeatherLocation, string>(
        new KeyValuePair<WeatherLocation, string>[] {
            new KeyValuePair<WeatherLocation, string>(WeatherLocation.Redmond, "47.67, -122.12"),
            new KeyValuePair<WeatherLocation, string>(WeatherLocation.LaQuinta, "33.66, -116.30"),
            new KeyValuePair<WeatherLocation, string>(WeatherLocation.Warsaw, "52.22, 21.01"),
            new KeyValuePair<WeatherLocation, string>(WeatherLocation.Pulawy, "51.25, 21.58"),
            new KeyValuePair<WeatherLocation, string>(WeatherLocation.Seoul, "37.53, 127.02"),
            new KeyValuePair<WeatherLocation, string>(WeatherLocation.LosAngeles, "34.05, -118.24"),
            new KeyValuePair<WeatherLocation, string>(WeatherLocation.SanLouisObispo, "35.27, -120.68"),
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