// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure;

namespace mochi;

public class WeatherClient
{
    static readonly HttpClient client = new HttpClient();

    string _sharedKey;

    public WeatherClient(SettingsClient settings)
    {
        _sharedKey = settings.GetSecret("mochi-maps-key");    
    }

    public (string Description, int TempF) GetCurrent(double latitude, double longitude)
    {
        // https://learn.microsoft.com/en-us/rest/api/maps/weather/get-daily-forecast?tabs=HTTP

        var coordinates = $"{latitude}, {longitude}";
        var uri = $"https://atlas.microsoft.com/weather/currentConditions/json?api-version=1.1&subscription-key={_sharedKey}&query={coordinates}&unit=imperial";
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
}