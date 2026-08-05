using GardenSystem.Application.Abstractions;
using GardenSystem.Infrastructure.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace GardenSystem.Infrastructure.Email;

public sealed class SmtpEmailSender(IOptions<SmtpOptions> smtpOptions) : IEmailSender
{
    public async Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        var options = smtpOptions.Value;

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse("no-reply@gardensystem.local"));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(options.Host, options.Port, SecureSocketOptions.None, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
