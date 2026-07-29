using FidelitasHub.Services.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FidelitasHub.Services.Email
{
    public class EmailService
    {
        private readonly ConfigurationService _configuration;
        private readonly EmailTemplateService _templateService;

        public EmailService(
            ConfigurationService configuration,
            EmailTemplateService templateService)
        {
            _configuration = configuration;
            _templateService = templateService;
        }

        //==================================================
        // Send Normal Email
        //==================================================

        public async Task SendEmailAsync(
            string toEmail,
            string subject,
            string body,
            bool isHtml = true)
        {
            var settings = _configuration.GetEmailSettings();

            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    settings.SenderName,
                    settings.SenderEmail));

            message.To.Add(
                MailboxAddress.Parse(toEmail));

            message.Subject = subject;

            message.Body = new TextPart(isHtml ? "html" : "plain")
            {
                Text = body
            };

            using var client = new SmtpClient();

            await client.ConnectAsync(
                settings.SmtpServer,
                settings.Port,
                settings.EnableSSL
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(
                settings.Username,
                settings.Password);

            await client.SendAsync(message);

            await client.DisconnectAsync(true);
        }

        //==================================================
        // Send Email Using Template
        //==================================================

        public async Task SendTemplateAsync(
            string templateCode,
            string toEmail,
            Dictionary<string, string> values)
        {
            var template =
                await _templateService.RenderTemplateAsync(
                    templateCode,
                    values);

            await SendEmailAsync(
                toEmail,
                template.Subject,
                template.Body,
                true);
        }
    }
}