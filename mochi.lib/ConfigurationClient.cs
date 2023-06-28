// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Security.KeyVault.Secrets;
using Azure.Data.AppConfiguration;
using Azure.Identity;

namespace mochi;

public class SettingsClient
{
    SecretClient keys;

    public SettingsClient(Uri keyVaultEndpoint)
    {
        var credential = new AzureCliCredential();
        keys = new SecretClient(keyVaultEndpoint, new AzureCliCredential());
    }

    public string GetSecret(string name)
    {
        KeyVaultSecret secret = keys.GetSecret(name);
        return secret.Value;
    }
}

