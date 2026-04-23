using System.Net;
using System.Net.Mail;
using The_App.Services.Interfaces;

namespace The_App.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendVerificationEmailAsync(string toEmail, string toName, string verificationLink)
        {
            try
            {
                var smtpHost = _config["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_config["EmailSettings:SmtpPort"] ?? "587");
                var smtpUser = _config["EmailSettings:SmtpUser"] ?? "";
                var smtpPass = _config["EmailSettings:SmtpPass"] ?? "";
                var fromAddress = _config["EmailSettings:FromAddress"] ?? smtpUser;
                var fromName = _config["EmailSettings:FromName"] ?? "THE APP";

                if (string.IsNullOrWhiteSpace(smtpUser) || string.IsNullOrWhiteSpace(smtpPass))
                {
                    _logger.LogError("SMTP credentials are not configured. Please update appsettings.json with EmailSettings:SmtpUser and EmailSettings:SmtpPass");
                    return;
                }

                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    _logger.LogError("Cannot send email: toEmail is empty");
                    return;
                }

                _logger.LogInformation("Sending verification email to {Email} using SMTP: {Host}:{Port}", toEmail, smtpHost, smtpPort);

                var message = new MailMessage
                {
                    From = new MailAddress(fromAddress, fromName),
                    Subject = "Verify your email address",
                    IsBodyHtml = true,
                    Body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px;'>
                        <h2>Email Verification</h2>
                        <p>Hello {WebUtility.HtmlEncode(toName)},</p>
                        <p>Thank you for registering. Please verify your email address by clicking the link below:</p>
                        <p><a href='{verificationLink}' style='background:#0d6efd;color:#fff;padding:10px 20px;text-decoration:none;border-radius:4px;display:inline-block;'>Verify Email</a></p>
                        <p>Or copy this link: <br/><a href='{verificationLink}'>{verificationLink}</a></p>
                        <p>If you did not register, please ignore this email.</p>
                        <hr style='border:none;border-top:1px solid #ddd;margin-top:20px;'/>
                        <p style='font-size:12px;color:#666;'>This is an automated email. Please do not reply.</p>
                    </div>"
                };
                message.To.Add(new MailAddress(toEmail, toName));

                using var smtp = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true,
                    Timeout = 10000
                };

                await smtp.SendMailAsync(message);
                _logger.LogInformation("✓ Verification email successfully sent to {Email}", toEmail);
            }
            catch (SmtpException smtpEx) when (smtpEx.Message.Contains("Authentication") || smtpEx.Message.Contains("5.7.0"))
            {
                _logger.LogError(smtpEx, "❌ SMTP AUTHENTICATION FAILED. For Gmail: " +
                    "1. Enable 2-Step Verification: https://myaccount.google.com/security " +
                    "2. Generate App Password: https://myaccount.google.com/apppasswords " +
                    "3. Use the 16-character App Password (not your regular Gmail password) in appsettings.json");
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "❌ SMTP Server Error: {Message}. Verify SMTP host ({Host}), port ({Port}), and SSL settings.", 
                    smtpEx.Message, _config["EmailSettings:SmtpHost"], _config["EmailSettings:SmtpPort"]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send verification email to {Email}: {Message}", toEmail, ex.Message);
            }
        }
    }
}
