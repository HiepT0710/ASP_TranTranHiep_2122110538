using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using TranTranHiep_2122110538.ViewModels;

namespace TranTranHiep_2122110538.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody);
}

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly SmtpSettings _smtp;

    public EmailService(ILogger<EmailService> logger, IOptions<SmtpSettings> smtpOptions)
    {
        _logger = logger;
        _smtp = smtpOptions.Value;
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(_smtp.Host) || string.IsNullOrWhiteSpace(_smtp.Username) || string.IsNullOrWhiteSpace(_smtp.Password))
        {
            _logger.LogInformation("SMTP chưa được cấu hình. Email to {To} | Subject: {Subject} | Body: {Body}", to, subject, htmlBody);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_smtp.FromEmail, _smtp.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);

        using var client = new SmtpClient(_smtp.Host, _smtp.Port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_smtp.Username, _smtp.Password)
        };

        await client.SendMailAsync(message);
    }
}
