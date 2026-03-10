using Application.Features.Email;
using Application.Features.Email.Dtos;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        public EmailSettings Settings { get; set; }

        public EmailService(IOptions<EmailSettings> options)
        {
            Settings = options.Value;
        }

        public async Task SendEmailAsync(IEnumerable<string> toEmails, string subject, string body)
        {
            var mailMeesage = new MailMessage
            {
                From = new MailAddress(Settings.SenderEmail, Settings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
                Priority = MailPriority.High
            };

            foreach(var email in toEmails)
            {
                if (!string.IsNullOrEmpty(email))
                {
                    mailMeesage.To.Add(email);
                }
            }

            using var client = new SmtpClient
            {
                Host = Settings.Host,
                Port = Settings.Port,
                EnableSsl = Settings.UseSSL,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(Settings.Username, Settings.Password)
            };

            await client.SendMailAsync(mailMeesage);
        }
    }
}
