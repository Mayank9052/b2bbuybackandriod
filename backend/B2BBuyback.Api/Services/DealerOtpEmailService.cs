// B2BBuyback.Api/Services/DealerOtpEmailService.cs
using System.Net;
using System.Net.Mail;
using B2BBuyback.Api.Interfaces;

namespace B2BBuyback.Api.Services
{
    public class DealerOtpEmailService : IDealerOtpEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<DealerOtpEmailService> _logger;

        public DealerOtpEmailService(
            IConfiguration config,
            ILogger<DealerOtpEmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendOtpEmailAsync(
            string toEmail,
            string dealerName,
            string otpCode,
            int expiryMinutes = 5)
        {
            var smtp     = _config.GetSection("Smtp");
            var host     = smtp["Host"]     ?? "smtp.office365.com";
            var port     = int.Parse(smtp["Port"] ?? "587");
            var user     = smtp["User"]     ?? throw new InvalidOperationException("SMTP User not configured");
            var password = smtp["Password"] ?? throw new InvalidOperationException("SMTP Password not configured");

            var html = $@"<!DOCTYPE html>
<html>
<head><meta charset='utf-8'/></head>
<body style='margin:0;padding:0;background:#f1f5f9;font-family:Segoe UI,Arial,sans-serif;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background:#f1f5f9;padding:32px 0;'>
    <tr><td align='center'>
      <table width='480' cellpadding='0' cellspacing='0'
             style='background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);'>

        <!-- Header -->
        <tr>
          <td style='background:#0b1929;padding:28px 32px;text-align:center;'>
            <div style='display:inline-block;background:#fbbf24;border-radius:50%;
                        width:56px;height:56px;line-height:56px;font-size:20px;
                        font-weight:900;color:#0b1929;text-align:center;'>BG</div>
            <div style='color:#fff;font-size:18px;font-weight:700;margin-top:12px;'>BGauss PI App</div>
            <div style='color:rgba(255,255,255,0.5);font-size:12px;margin-top:4px;'>
              Procurement &amp; Inspection · Certified Exchange
            </div>
          </td>
        </tr>

        <!-- Body -->
        <tr>
          <td style='padding:32px;'>
            <p style='margin:0 0 8px;font-size:15px;color:#334155;'>
              Hi <strong>{dealerName}</strong>,
            </p>
            <p style='margin:0 0 24px;font-size:14px;color:#64748b;line-height:1.6;'>
              Your One-Time Password (OTP) for BGauss PI App login is:
            </p>

            <!-- OTP Box -->
            <div style='background:#fef3c7;border:2px solid #fbbf24;border-radius:12px;
                        padding:24px;text-align:center;margin:0 0 24px;'>
              <div style='font-size:42px;font-weight:900;letter-spacing:12px;
                          color:#0b1929;font-family:Courier New,monospace;'>
                {otpCode}
              </div>
              <div style='color:#92400e;font-size:12px;margin-top:10px;'>
                Valid for <strong>{expiryMinutes} minutes</strong> only
              </div>
            </div>

            <!-- Warning -->
            <div style='background:#fef2f2;border-left:4px solid #ef4444;
                        border-radius:6px;padding:12px 16px;margin:0 0 24px;'>
              <p style='margin:0;font-size:13px;color:#991b1b;'>
                Do not share this OTP with anyone.
                BGauss staff will never ask for your OTP.
              </p>
            </div>

            <p style='margin:0;font-size:13px;color:#94a3b8;line-height:1.6;'>
              If you did not request this OTP, please ignore this email or
              contact BGauss support at help@bgauss.com
            </p>
          </td>
        </tr>

        <!-- Footer -->
        <tr>
          <td style='background:#f8fafc;border-top:1px solid #e2e8f0;
                      padding:16px 32px;text-align:center;'>
            <p style='margin:0;font-size:11px;color:#94a3b8;'>
              &copy; {DateTime.UtcNow.Year} BGauss Auto Pvt. Ltd. All rights reserved.
            </p>
          </td>
        </tr>

      </table>
    </td></tr>
  </table>
</body>
</html>";

            // Office365 uses STARTTLS on port 587
            using var client = new SmtpClient(host, port)
            {
                EnableSsl             = true,
                DeliveryMethod        = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials           = new NetworkCredential(user, password),
                Timeout               = 15000,
            };

            using var message = new MailMessage
            {
                From       = new MailAddress(user, "BGauss PI App"),
                Subject    = $"Your BGauss OTP: {otpCode}",
                Body       = html,
                IsBodyHtml = true,
            };
            message.To.Add(new MailAddress(toEmail, dealerName));

            // Plain-text fallback
            var plainText = $"Hi {dealerName},\n\n" +
                            $"Your BGauss PI App OTP is: {otpCode}\n\n" +
                            $"Valid for {expiryMinutes} minutes. Do not share this OTP.\n\n" +
                            $"BGauss Auto Pvt. Ltd.";

            message.AlternateViews.Add(
                AlternateView.CreateAlternateViewFromString(plainText, null, "text/plain"));

            await client.SendMailAsync(message);

            _logger.LogInformation(
                "OTP email sent to {Email} for dealer {Name}", toEmail, dealerName);
        }
    }
}