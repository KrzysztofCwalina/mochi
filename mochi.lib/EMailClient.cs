// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace mochi.fx;

public class EMailClient
{
    SmtpClient client = new SmtpClient();
    string _from;

    public EMailClient(SettingsClient settings)
    {
        _from = settings.GetSecret("mochi-email-address"); 
        var password = settings.GetSecret("mochi-email-password");
        client.Connect("smtp.office365.com", 587, SecureSocketOptions.StartTls);
        client.Authenticate("mochiassistant@outlook.com", password);
    }

    public void Send(string to, string subject, string message)
    {
        var m = new MimeMessage();
        m.From.Add(new MailboxAddress("Mochi", _from));
        m.To.Add(new MailboxAddress("Mochi Fan", to));
        m.Subject = subject;
        m.Body = new TextPart("plain")
        {
            Text = message
        };

        client.Send(m);
    }
}