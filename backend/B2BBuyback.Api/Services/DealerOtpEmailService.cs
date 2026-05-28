using System.Net;
using System.Net.Mail;
using System.Net.Mime;
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
            var smtp = _config.GetSection("Smtp");

            var host = smtp["Host"] ?? "smtp.office365.com";
            var port = int.Parse(smtp["Port"] ?? "587");
            var user = smtp["User"];
            var password = smtp["Password"];

            if (string.IsNullOrEmpty(user))
                throw new Exception("SMTP User missing");

            if (string.IsNullOrEmpty(password))
                throw new Exception("SMTP Password missing");


            string html = $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='UTF-8'>
</head>

<body style='margin:0;padding:0;background:#f1f5f9;font-family:Arial,sans-serif;'>

<table width='100%' cellpadding='0' cellspacing='0' bgcolor='#f1f5f9'>
<tr>
<td align='center' style='padding:30px;'>

<table width='480' cellpadding='0' cellspacing='0'
style='background:#ffffff;border-radius:16px;'>

<tr>
<td bgcolor='#0b1929'
style='padding:25px;text-align:center;'>

<div style='background:#fbbf24;
width:60px;
height:60px;
line-height:60px;
margin:auto;
border-radius:50%;
font-weight:bold;
font-size:22px;'>
BG
</div>

<h2 style='color:white;margin-top:10px;'>
BGauss PI App
</h2>

<p style='color:#d1d5db;font-size:12px;'>
Procurement & Inspection
</p>

</td>
</tr>

<tr>
<td style='padding:30px;'>

<p>Hello <b>{dealerName}</b>,</p>

<p>
Your OTP for login:
</p>

<div style='background:#fef3c7;
border:2px solid #fbbf24;
padding:20px;
text-align:center;
border-radius:10px;
margin:20px 0;'>

<div style='font-size:35px;
font-weight:bold;
letter-spacing:8px;'>

{otpCode}

</div>

<p>
Valid for <b>{expiryMinutes} minutes</b>
</p>

</div>

<div style='background:#fef2f2;
padding:10px;
border-left:4px solid red;'>

Do not share this OTP with anyone.

</div>

<p style='margin-top:20px;color:gray;font-size:12px;'>

If you didn't request this OTP,
please ignore this email.

</p>

</td>
</tr>

<tr>
<td style='padding:15px;
background:#f8fafc;
text-align:center;
font-size:12px;
color:gray;'>

© {DateTime.Now.Year} BGauss Auto Pvt Ltd

</td>
</tr>

</table>

</td>
</tr>
</table>

</body>
</html>";

            string plainText =
$@"Hello {dealerName},

Your OTP is: {otpCode}

Valid for {expiryMinutes} minutes.

Do not share this OTP.

BGauss Auto Pvt Ltd";


            using var mail = new MailMessage();

            mail.From = new MailAddress(
                user,
                "BGauss PI App");

            mail.To.Add(toEmail);

            mail.Subject = $"Your BGauss OTP: {otpCode}";

            // Important fix
            var plainView = AlternateView.CreateAlternateViewFromString(
                plainText,
                null,
                MediaTypeNames.Text.Plain);

            var htmlView = AlternateView.CreateAlternateViewFromString(
                html,
                null,
                MediaTypeNames.Text.Html);

            mail.AlternateViews.Add(plainView);
            mail.AlternateViews.Add(htmlView);

            using var smtpClient = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(user, password),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            await smtpClient.SendMailAsync(mail);

            _logger.LogInformation(
                "OTP sent to {email}",
                toEmail);
        }
    }
}