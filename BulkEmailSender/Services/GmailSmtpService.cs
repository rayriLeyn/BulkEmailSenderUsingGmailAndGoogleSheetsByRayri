using BulkEmailSender.Config;
using BulkEmailSender.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Net.Mail;

namespace BulkEmailSender.Services;

public class GmailSmtpService
{
    private readonly GmailSettings _config;

    public GmailSmtpService(GmailSettings config)
    {
        _config = config;
    }

    public async Task SendAsync(Recipient recipient, string htmlTemplate)
    {
        // Replace placeholders with recipient data
        var html = htmlTemplate
            .Replace("{{name}}", recipient.Name)
            .Replace("{{email}}", recipient.Email)
         .Replace("{{position}}", recipient.Position);  

        // Build the email message
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_config.SenderName, _config.SenderEmail));
        message.To.Add(new MailboxAddress(recipient.Name, recipient.Email));
        message.Subject = "Welcome to AWS Cloud Club - PUP Binan '26!";

        var bodyBuilder = new BodyBuilder { HtmlBody = html };
        message.Body = bodyBuilder.ToMessageBody();

        // Connect and send via Gmail SMTP
        using var client = new MailKit.Net.Smtp.SmtpClient();

        await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_config.SenderEmail, _config.AppPassword);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        Console.WriteLine($"✅ Sent   → {recipient.Email}");
    }
}