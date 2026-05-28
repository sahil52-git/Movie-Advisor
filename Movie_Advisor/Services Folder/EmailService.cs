using System.Net;
using System.Net.Mail;

namespace Movie_Advisor.Services
{
    // ========================================
    // INTERFACE - Email Service Contract
    // ========================================

    /// <summary>
    /// Public interface for email service operations
    /// </summary>
    public interface IEmailService
    {
        Task<bool> SendOtpEmailAsync(string toEmail, string otp, string username);
        Task<bool> SendPasswordResetConfirmationAsync(string toEmail, string username);
        Task<bool> SendWelcomeEmailAsync(string toEmail, string username);
    }

    // ========================================
    // IMPLEMENTATION - Email Service
    // ========================================

    /// <summary>
    /// Email service implementation using SMTP
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendOtpEmailAsync(string toEmail, string otp, string username)
        {
            try
            {
                var subject = "🔐 Your Password Reset OTP - Movie Advisor";
                var body = GetOtpEmailTemplate(username, otp);
                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendPasswordResetConfirmationAsync(string toEmail, string username)
        {
            try
            {
                var subject = "✅ Password Reset Successful - Movie Advisor";
                var body = GetPasswordResetConfirmationTemplate(username);
                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset confirmation to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendWelcomeEmailAsync(string toEmail, string username)
        {
            try
            {
                var subject = "🎬 Welcome to Movie Advisor!";
                var body = GetWelcomeEmailTemplate(username);
                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to {Email}", toEmail);
                return false;
            }
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
                var fromEmail = _configuration["Email:FromEmail"] ?? throw new InvalidOperationException("Email:FromEmail not configured");
                var fromName = _configuration["Email:FromName"] ?? "Movie Advisor";
                var password = _configuration["Email:Password"] ?? throw new InvalidOperationException("Email:Password not configured");

                using var smtpClient = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromEmail, password),
                    Timeout = 30000
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true,
                    Priority = MailPriority.High
                };

                mailMessage.To.Add(toEmail);
                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("Email sent successfully to {Email}", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email}", toEmail);
                return false;
            }
        }

        private string GetOtpEmailTemplate(string username, string otp)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: linear-gradient(135deg, #0f2862 0%, #091f36 100%); margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #1e232d; border-radius: 20px; overflow: hidden; box-shadow: 0 20px 50px rgba(0, 0, 0, 0.5); }}
        .header {{ background: linear-gradient(135deg, #00bcd4 0%, #0097a7 100%); padding: 30px; text-align: center; }}
        .header h1 {{ color: #000; margin: 0; font-size: 28px; font-weight: 700; }}
        .content {{ padding: 40px 30px; color: #fff; }}
        .greeting {{ font-size: 20px; margin-bottom: 20px; color: #00bcd4; }}
        .message {{ font-size: 16px; line-height: 1.6; margin-bottom: 30px; color: rgba(255, 255, 255, 0.9); }}
        .otp-box {{ background: rgba(0, 188, 212, 0.1); border: 2px solid #00bcd4; border-radius: 12px; padding: 30px; text-align: center; margin: 30px 0; }}
        .otp-code {{ font-size: 42px; font-weight: 700; color: #00bcd4; letter-spacing: 8px; margin: 0; font-family: 'Courier New', monospace; }}
        .otp-label {{ color: rgba(255, 255, 255, 0.7); font-size: 14px; margin-top: 10px; }}
        .warning {{ background: rgba(255, 107, 107, 0.1); border-left: 4px solid #ff6b6b; padding: 15px; margin: 20px 0; border-radius: 8px; }}
        .warning p {{ margin: 0; color: #ff6b6b; font-size: 14px; }}
        .footer {{ background: rgba(0, 0, 0, 0.2); padding: 20px 30px; text-align: center; color: rgba(255, 255, 255, 0.6); font-size: 12px; }}
        .expiry {{ color: #00bcd4; font-weight: 600; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'><h1>🎬 Movie Advisor</h1></div>
        <div class='content'>
            <div class='greeting'>Hello {username}! 👋</div>
            <div class='message'>We received a request to reset your password. Use the OTP code below to proceed with resetting your password.</div>
            <div class='otp-box'>
                <div class='otp-code'>{otp}</div>
                <div class='otp-label'>Your One-Time Password</div>
            </div>
            <div class='message'>This OTP will expire in <span class='expiry'>10 minutes</span>. If you didn't request a password reset, please ignore this email and your password will remain unchanged.</div>
            <div class='warning'><p>⚠️ <strong>Security Notice:</strong> Never share this OTP with anyone. Movie Advisor staff will never ask for your OTP.</p></div>
        </div>
        <div class='footer'>
            <p>© 2024 Movie Advisor. All rights reserved.</p>
            <p>This is an automated email. Please do not reply.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GetPasswordResetConfirmationTemplate(string username)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: linear-gradient(135deg, #0f2862 0%, #091f36 100%); margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #1e232d; border-radius: 20px; overflow: hidden; box-shadow: 0 20px 50px rgba(0, 0, 0, 0.5); }}
        .header {{ background: linear-gradient(135deg, #4CAF50 0%, #2e7d32 100%); padding: 30px; text-align: center; }}
        .header h1 {{ color: #fff; margin: 0; font-size: 28px; }}
        .content {{ padding: 40px 30px; color: #fff; text-align: center; }}
        .success-icon {{ font-size: 72px; margin-bottom: 20px; }}
        .message {{ font-size: 18px; line-height: 1.6; margin: 20px 0; }}
        .footer {{ background: rgba(0, 0, 0, 0.2); padding: 20px; text-align: center; color: rgba(255, 255, 255, 0.6); font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'><h1>🎬 Movie Advisor</h1></div>
        <div class='content'>
            <div class='success-icon'>✅</div>
            <div class='message'><strong>Hi {username}!</strong><br><br>Your password has been successfully reset. You can now log in with your new password.<br><br>If you didn't make this change, please contact our support team immediately.</div>
        </div>
        <div class='footer'><p>© 2024 Movie Advisor. All rights reserved.</p></div>
    </div>
</body>
</html>";
        }

        private string GetWelcomeEmailTemplate(string username)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: linear-gradient(135deg, #0f2862 0%, #091f36 100%); margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #1e232d; border-radius: 20px; overflow: hidden; }}
        .header {{ background: linear-gradient(135deg, #00bcd4 0%, #0097a7 100%); padding: 40px; text-align: center; }}
        .content {{ padding: 40px 30px; color: #fff; }}
        .footer {{ background: rgba(0, 0, 0, 0.2); padding: 20px; text-align: center; color: rgba(255, 255, 255, 0.6); font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'><h1 style='color: #000; margin: 0;'>🎬 Welcome to Movie Advisor!</h1></div>
        <div class='content'>
            <h2 style='color: #00bcd4;'>Hello {username}! 🎉</h2>
            <p>Thank you for joining Movie Advisor. Get ready to discover amazing movies tailored just for you!</p>
        </div>
        <div class='footer'><p>© 2024 Movie Advisor. All rights reserved.</p></div>
    </div>
</body>
</html>";
        }
    }
}