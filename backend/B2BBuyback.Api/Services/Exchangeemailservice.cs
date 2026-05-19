// Services/ExchangeEmailService.cs
//
// ══════════════════════════════════════════════════════════════════════════════
//  EMAIL FLOW — FINAL
//
//  SMTP AUTH    : priyanka.nikam@bgauss.com  (fixed Office365 credentials)
//  Sender header: priyanka.nikam@bgauss.com  (required by Office365 SMTP AUTH)
//
//  ┌──────────────────────────────────────────────────────────────────────────┐
//  │ Email Type           │ FROM (display)      │ TO               │ REPLY-TO │
//  ├──────────────────────────────────────────────────────────────────────────┤
//  │ Dealer ACK           │ dealer.Email (DB)   │ dealer.Email     │ AdminEmail│
//  │ Admin Notification   │ dealer.Email (DB)   │ AdminEmail       │ dealer.Email│
//  │ Decision → Dealer    │ AdminEmail          │ dealer.Email (DB)│ AdminEmail│
//  └──────────────────────────────────────────────────────────────────────────┘
//
//  Office365 STARTTLS note:
//    • SmtpClient.EnableSsl = true  → triggers STARTTLS on port 587
//    • Do NOT use port 465 (SSL)
//    • ServicePointManager.SecurityProtocol is set to Tls12 for compatibility
//
//  "FROM = dealer email" technique:
//    • mail.From    = dealer.Email   (what recipient sees in their inbox)
//    • mail.Sender  = smtp auth user (what Office365 actually sends from)
//    • Office365 adds "on behalf of" in some clients — this is expected behaviour
//      for shared-mailbox / delegated send patterns on basic SMTP AUTH.
// ══════════════════════════════════════════════════════════════════════════════

using System.Net;
using System.Net.Mail;
using System.Security.Authentication;
using B2BBuyback.Api.Interfaces;
using B2BBuyback.Api.Models;

namespace B2BBuyback.Api.Services;

public class ExchangeEmailService : IExchangeEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<ExchangeEmailService> _logger;

    // ── Config shortcuts ──────────────────────────────────────────────────────
    private string SmtpHost     => _config["Smtp:Host"]     ?? "smtp.office365.com";
    private int    SmtpPort     => int.TryParse(_config["Smtp:Port"], out var p) ? p : 587;
    private string SmtpUser     => _config["Smtp:User"]     ?? "";   // SMTP auth account
    private string SmtpPassword => _config["Smtp:Password"] ?? "";
    private string AdminEmail   => _config["Exchange:AdminEmail"] ?? SmtpUser;
    private const  string AdminDisplayName = "BGauss Exchange Admin";

    public ExchangeEmailService(
        IConfiguration config,
        ILogger<ExchangeEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    // ── SMTP client factory ───────────────────────────────────────────────────
    // EnableSsl = true  → STARTTLS handshake on port 587 (Office365 requirement)
    private SmtpClient BuildClient()
    {
        // Ensure TLS 1.2 is allowed (SSLv23 / TlsAuto maps to this on .NET 6+)
        ServicePointManager.SecurityProtocol =
            SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

        return new SmtpClient(SmtpHost, SmtpPort)
        {
            Credentials           = new NetworkCredential(SmtpUser, SmtpPassword),
            EnableSsl             = true,          // STARTTLS on port 587
            DeliveryMethod        = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
        };
    }

    // ── Build a MailMessage with FROM = senderEmail (dealer or admin) ─────────
    // Sender (technical) = SmtpUser (required for Office365 SMTP AUTH)
    // From   (display)   = senderEmail  (what the recipient sees)
    // Reply-To           = replyToEmail
    private MailMessage BuildMail(
        string fromEmail,
        string fromDisplayName,
        string toEmail,
        string toDisplayName,
        string subject,
        string htmlBody,
        string replyToEmail,
        string replyToDisplayName,
        string? ccEmail = null)
    {
        var mail = new MailMessage
        {
            // ── FROM: dealer's registered email (display only) ──────────────
            // Office365 will show "on behalf of" in Outlook if From ≠ SmtpUser,
            // but the email WILL be delivered. This is the standard behaviour
            // for delegated/shared mailbox sends on basic SMTP AUTH.
            From       = new MailAddress(fromEmail, fromDisplayName),

            // ── Sender: always the SMTP auth account (Office365 requirement) ─
            Sender     = new MailAddress(SmtpUser, "BGauss Exchange"),

            Subject    = subject,
            Body       = htmlBody,
            IsBodyHtml = true,
        };

        mail.To.Add(new MailAddress(toEmail, toDisplayName));

        // Reply-To — so the recipient can reply to the right person
        mail.ReplyToList.Add(new MailAddress(replyToEmail, replyToDisplayName));

        // Optional CC (used for admin notification CC)
        if (!string.IsNullOrWhiteSpace(ccEmail) &&
            !ccEmail.Equals(toEmail, StringComparison.OrdinalIgnoreCase))
            mail.CC.Add(ccEmail);

        return mail;
    }

    // ── Validate email ────────────────────────────────────────────────────────
    private static bool HasValidEmail(User dealer) =>
        !string.IsNullOrWhiteSpace(dealer.Email) && dealer.Email.Contains("@");

    // ── AppUrl for deep-link buttons ──────────────────────────────────────────
    private string AppUrl => (_config["AppUrl"] ?? "").TrimEnd('/');

    // ═════════════════════════════════════════════════════════════════════════
    //  PUBLIC INTERFACE
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called when dealer submits a case.
    /// 1. ACK email → TO dealer, FROM dealer's email, Reply-To admin
    /// 2. Notification → TO admin, FROM dealer's email, Reply-To dealer
    /// </summary>
    public async Task SendCaseSubmissionEmailsAsync(ExchangeCase c, User dealer)
    {
        await SendDealerAckAsync(c, dealer);
        await SendAdminNotificationAsync(c, dealer);
    }

    /// <summary>
    /// Called when admin approves / modifies / rejects.
    /// Decision email → TO dealer, FROM AdminEmail, Reply-To admin
    /// </summary>
    public async Task SendAdminActionEmailAsync(
        ExchangeCase c, User dealer, string action, string? note)
    {
        if (!HasValidEmail(dealer))
        {
            _logger.LogWarning(
                "Skipping decision email for {Code} — dealer has no valid email",
                c.CaseNumber);
            return;
        }

        try
        {
            var (subject, accent, heading) = action switch
            {
                "Approved" => (
                    $"[BGauss Exchange] ✅ Case Approved — {c.CaseNumber}",
                    "#16A34A", "✅ Exchange Case Approved"),
                "Modified" => (
                    $"[BGauss Exchange] 🔄 Price Modified — {c.CaseNumber}",
                    "#D97706", "🔄 Exchange Case — Price Modified by Admin"),
                _ => (
                    $"[BGauss Exchange] ❌ Case Rejected — {c.CaseNumber}",
                    "#DC2626", "❌ Exchange Case Rejected"),
            };

            using var client = BuildClient();

            // Decision email: FROM admin, TO dealer, Reply-To admin
            var mail = BuildMail(
                fromEmail:          AdminEmail,
                fromDisplayName:    AdminDisplayName,
                toEmail:            dealer.Email,
                toDisplayName:      dealer.FullName,
                subject:            subject,
                htmlBody:           DecisionHtml(c, dealer, action, note, accent, heading),
                replyToEmail:       AdminEmail,
                replyToDisplayName: AdminDisplayName);

            await client.SendMailAsync(mail);

            _logger.LogInformation(
                "✅ Decision ({Action}) sent → FROM: {Admin} | TO: {Email} ({Name}) | Case: {Code}",
                action, AdminEmail, dealer.Email, dealer.FullName, c.CaseNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ Decision email failed → TO: {Email} | Case: {Code}",
                dealer.Email, c.CaseNumber);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  PRIVATE SENDERS
    // ═════════════════════════════════════════════════════════════════════════

    // ── 1. Dealer ACK ─────────────────────────────────────────────────────────
    // FROM (display) : dealer.Email   ← user's registered email
    // TO             : dealer.Email
    // Reply-To       : AdminEmail     ← admin can reply to dealer's query
    private async Task SendDealerAckAsync(ExchangeCase c, User dealer)
    {
        if (!HasValidEmail(dealer))
        {
            _logger.LogWarning(
                "Skipping dealer ACK for {Code} — no valid email for {Name}",
                c.CaseNumber, dealer.FullName);
            return;
        }

        try
        {
            using var client = BuildClient();

            var mail = BuildMail(
                fromEmail:          dealer.Email,       // ← FROM dealer's own email
                fromDisplayName:    dealer.FullName,
                toEmail:            dealer.Email,
                toDisplayName:      dealer.FullName,
                subject:            $"[BGauss Exchange] ✅ Case Submitted — {c.CaseNumber}",
                htmlBody:           DealerAckHtml(c, dealer),
                replyToEmail:       AdminEmail,
                replyToDisplayName: AdminDisplayName);

            await client.SendMailAsync(mail);

            _logger.LogInformation(
                "✅ ACK sent → FROM: {From} | TO: {To} ({Name}) | Case: {Code}",
                dealer.Email, dealer.Email, dealer.FullName, c.CaseNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ ACK email failed → TO: {Email} | Case: {Code}",
                dealer.Email, c.CaseNumber);
        }
    }

    // ── 2. Admin notification ─────────────────────────────────────────────────
    // FROM (display) : dealer.Email   ← admin sees "email from the dealer"
    // TO             : AdminEmail
    // Reply-To       : dealer.Email   ← admin clicks Reply → goes to dealer
    private async Task SendAdminNotificationAsync(ExchangeCase c, User dealer)
    {
        try
        {
            using var client = BuildClient();

            var mail = BuildMail(
                fromEmail:          dealer.Email,       // ← FROM dealer's email
                fromDisplayName:    $"{dealer.FullName} (BGauss Exchange)",
                toEmail:            AdminEmail,
                toDisplayName:      AdminDisplayName,
                subject:            $"[BGauss Exchange] ⚡ Action Required — {c.CaseNumber} | {dealer.FullName}",
                htmlBody:           AdminNotificationHtml(c, dealer),
                replyToEmail:       HasValidEmail(dealer) ? dealer.Email : AdminEmail,
                replyToDisplayName: dealer.FullName);

            await client.SendMailAsync(mail);

            _logger.LogInformation(
                "✅ Admin notification sent → FROM: {From} | TO: {Admin} | Dealer: {Name} | Case: {Code}",
                dealer.Email, AdminEmail, dealer.FullName, c.CaseNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ Admin notification failed | Case: {Code}", c.CaseNumber);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  HTML TEMPLATE HELPERS
    // ═════════════════════════════════════════════════════════════════════════

    private static string H(string? s)        => WebUtility.HtmlEncode(s ?? "");
    private static string Fmt(decimal? price) => price.HasValue ? $"&#8377;&nbsp;{price.Value:N0}" : "—";

    private static string Row(string label, string value) => $"""
        <tr>
          <td style="padding:8px 0;color:#6b7280;width:190px;vertical-align:top;font-size:13px">{label}</td>
          <td style="padding:8px 0;font-weight:600;color:#0f172a;font-size:13px">{value}</td>
        </tr>
        """;

    private static string Wrap(string accent, string heading, string body) => $"""
        <html><body style="margin:0;padding:20px;background:#f1f5f9;font-family:'Segoe UI',Arial,sans-serif">
          <div style="max-width:640px;margin:auto;background:#fff;border-radius:14px;overflow:hidden;
                      border:1px solid #e2e8f0;box-shadow:0 4px 16px rgba(0,0,0,0.07)">
            <div style="background:{accent};padding:28px 32px">
              <p style="margin:0 0 4px;color:rgba(255,255,255,0.8);font-size:11px;
                         letter-spacing:1px;text-transform:uppercase">
                BGauss Exchange &amp; Buyback Program
              </p>
              <h2 style="margin:0;color:#fff;font-size:20px;font-weight:700">{heading}</h2>
            </div>
            <div style="padding:28px 32px">{body}</div>
            <div style="background:#f8fafc;padding:14px 32px;font-size:11px;color:#94a3b8;
                         border-top:1px solid #e2e8f0;text-align:center">
              BGauss Exchange &amp; Buyback System &nbsp;·&nbsp; Auto-generated
            </div>
          </div>
        </body></html>
        """;

    private static string InfoBox(string bg, string border, string color, string text) =>
        $"""
        <div style="margin-top:20px;padding:14px 18px;background:{bg};border:1px solid {border};
                    border-radius:8px;font-size:13px;color:{color}">
          {text}
        </div>
        """;

    private string ActionBtn(int caseId, string action, string label, string bg, string emoji) =>
        $"""
        <a href="{AppUrl}/exchange-admin?caseId={caseId}&action={WebUtility.UrlEncode(action)}"
           style="display:inline-block;background:{bg};color:#fff;padding:12px 22px;margin:4px 6px 4px 0;
                  border-radius:8px;text-decoration:none;font-size:14px;font-weight:700;
                  letter-spacing:0.2px;box-shadow:0 2px 8px rgba(0,0,0,0.15)">
          {emoji} {label}
        </a>
        """;

    private string CTA(string path, string label, string color) =>
        $"""
        <a href="{AppUrl}{path}"
           style="display:inline-block;background:{color};color:#fff;padding:11px 24px;
                  border-radius:8px;text-decoration:none;font-size:14px;font-weight:600">
          {label}
        </a>
        """;

    // ── Template 1: Dealer ACK ────────────────────────────────────────────────
    private string DealerAckHtml(ExchangeCase c, User dealer) =>
        Wrap("#1D4ED8", "Exchange Case Submitted", $"""
            <p style="margin:0 0 20px;color:#374151;font-size:14px">
              Hi <strong>{H(dealer.FullName)}</strong>,<br/><br/>
              Your exchange case has been successfully submitted and is now
              <strong>pending Admin review</strong>.<br/>
              You will receive another email once a decision is made.
            </p>
            <table style="width:100%;border-collapse:collapse;border-top:1px solid #e2e8f0">
              {Row("Case Number",      H(c.CaseNumber))}
              {Row("Customer Name",    H(c.CustomerName))}
              {Row("Mobile",           H(c.MobileNumber))}
              {Row("City",             H(c.City))}
              {Row("Vehicle Model",    H(c.VehicleModel))}
              {Row("Registration No.", H(c.RegistrationNo))}
              {Row("Year of Purchase", c.YearOfPurchase.ToString())}
              {Row("KM Driven",        $"{c.KmDriven:N0} km")}
              {Row("Inspection Grade", H(c.Grade))}
              {Row("Total Score",      c.TotalScore.HasValue ? $"{c.TotalScore:F1} / 10" : "—")}
              {Row("Price Range",      $"{Fmt(c.MinPrice)} – {Fmt(c.MaxPrice)}")}
              {Row("Recommended",      Fmt(c.RecommendedPrice))}
              {Row("Submitted At",     (c.SubmittedAt ?? DateTime.UtcNow).ToString("dd MMM yyyy, hh:mm tt") + " UTC")}
            </table>
            {InfoBox("#eff6ff", "#bfdbfe", "#1e40af",
                "ℹ️ The system-generated price range has been forwarded to Admin for review.")}
            <div style="margin-top:24px">
              {CTA($"/exchange/cases/{c.Id}", "View Case →", "#1D4ED8")}
            </div>
            """);

    // ── Template 2: Admin notification ───────────────────────────────────────
    private string AdminNotificationHtml(ExchangeCase c, User dealer) =>
        Wrap("#D97706", $"⚡ Action Required — {c.CaseNumber}", $"""
            <p style="margin:0 0 6px;color:#374151;font-size:14px">
              Hi <strong>Admin</strong>,
            </p>
            <p style="margin:0 0 20px;color:#374151;font-size:14px">
              A new exchange case was submitted by dealer
              <strong>{H(dealer.FullName)}</strong>.
              Use the buttons below to take action.
            </p>

            <!-- Dealer Info -->
            <div style="margin-bottom:24px;padding:16px 20px;background:#f0f9ff;
                        border:1px solid #bae6fd;border-radius:10px">
              <p style="margin:0 0 10px;font-weight:700;font-size:11px;
                         letter-spacing:0.8px;text-transform:uppercase;color:#0369a1">
                👤 Dealer Information
              </p>
              <table style="width:100%;border-collapse:collapse">
                {Row("Full Name",   H(dealer.FullName))}
                {Row("Email",       $"<a href='mailto:{H(dealer.Email)}' style='color:#1d4ed8'>{H(dealer.Email)}</a>")}
                {Row("Phone",       H(dealer.PhoneNumber ?? "—"))}
                {Row("Employee ID", H(dealer.EmployeeId  ?? "—"))}
                {Row("Department",  H(dealer.Department  ?? "—"))}
              </table>
            </div>

            <!-- Case Details -->
            <table style="width:100%;border-collapse:collapse;border-top:1px solid #e2e8f0">
              {Row("Case Number",      H(c.CaseNumber))}
              {Row("Customer Name",    H(c.CustomerName))}
              {Row("Mobile",           H(c.MobileNumber))}
              {Row("City",             H(c.City))}
              {Row("Vehicle Model",    H(c.VehicleModel))}
              {Row("Registration No.", H(c.RegistrationNo))}
              {Row("Year of Purchase", c.YearOfPurchase.ToString())}
              {Row("KM Driven",        $"{c.KmDriven:N0} km")}
              {Row("Inspection Grade", H(c.Grade))}
              {Row("Total Score",      c.TotalScore.HasValue ? $"{c.TotalScore:F1} / 10" : "—")}
              {Row("Min Price",        Fmt(c.MinPrice))}
              {Row("Recommended",      Fmt(c.RecommendedPrice))}
              {Row("Max Price",        Fmt(c.MaxPrice))}
              {Row("Submitted At",     (c.SubmittedAt ?? DateTime.UtcNow).ToString("dd MMM yyyy, hh:mm tt") + " UTC")}
            </table>

            <!-- One-Click Action Buttons -->
            <div style="margin-top:28px;padding:20px;background:#f8fafc;border-radius:10px;
                        border:1px solid #e2e8f0">
              <p style="margin:0 0 6px;font-weight:700;font-size:14px;color:#0f172a">
                ⚡ Take Action
              </p>
              <p style="margin:0 0 16px;font-size:12px;color:#6b7280">
                Click a button to open the admin portal with this case pre-selected.
              </p>
              <div>
                {ActionBtn(c.Id, "Approved", "Approve Case", "#16A34A", "✅")}
                {ActionBtn(c.Id, "Modified", "Modify Price", "#D97706", "✏️")}
                {ActionBtn(c.Id, "Rejected", "Reject Case",  "#DC2626", "❌")}
              </div>
            </div>
            """);

    // ── Template 3: Decision to dealer ───────────────────────────────────────
    private string DecisionHtml(
        ExchangeCase c, User dealer,
        string action, string? note,
        string accent, string heading)
    {
        var intro = action switch
        {
            "Approved" =>
                $"Your exchange case <strong>{H(c.CaseNumber)}</strong> has been " +
                $"<strong style=\"color:#16A34A\">approved</strong> by Admin.",
            "Modified" =>
                $"Your exchange case <strong>{H(c.CaseNumber)}</strong> has been reviewed. " +
                $"Admin has <strong style=\"color:#D97706\">modified the approved price</strong>.",
            _ =>
                $"Your exchange case <strong>{H(c.CaseNumber)}</strong> has been " +
                $"<strong style=\"color:#DC2626\">rejected</strong> by Admin.",
        };

        var banner = action switch
        {
            "Approved" => InfoBox("#f0fdf4", "#bbf7d0", "#166534",
                $"✅ Approved. Proceed at the approved price of <strong>{Fmt(c.ApprovedPrice)}</strong>."),
            "Modified" => InfoBox("#fefce8", "#fde68a", "#92400e",
                $"🔄 Admin set a modified price of <strong>{Fmt(c.ApprovedPrice)}</strong>."),
            _ => InfoBox("#fef2f2", "#fecaca", "#991b1b",
                "❌ This case has been rejected. Contact BGauss if you have questions."),
        };

        return Wrap(accent, heading, $"""
            <p style="margin:0 0 20px;color:#374151;font-size:14px">
              Hi <strong>{H(dealer.FullName)}</strong>,<br/><br/>
              {intro}
            </p>
            <table style="width:100%;border-collapse:collapse;border-top:1px solid #e2e8f0">
              {Row("Case Number",        H(c.CaseNumber))}
              {Row("Customer Name",      H(c.CustomerName))}
              {Row("Vehicle Model",      H(c.VehicleModel))}
              {Row("Registration No.",   H(c.RegistrationNo))}
              {Row("Inspection Grade",   H(c.Grade))}
              {Row("Total Score",        c.TotalScore.HasValue ? $"{c.TotalScore:F1} / 10" : "—")}
              {Row("System Min",         Fmt(c.MinPrice))}
              {Row("System Recommended", Fmt(c.RecommendedPrice))}
              {Row("System Max",         Fmt(c.MaxPrice))}
              {(action != "Rejected"
                  ? Row("Admin Approved Price",
                        $"<span style=\"color:{accent};font-size:16px;font-weight:800\">" +
                        $"{Fmt(c.ApprovedPrice)}</span>")
                  : "")}
              {Row("Status",    H(c.Status))}
              {Row("Decision At",
                   (c.AdminActionAt ?? DateTime.UtcNow).ToString("dd MMM yyyy, hh:mm tt") + " UTC")}
              {(string.IsNullOrWhiteSpace(note) ? "" : Row("Admin Remarks", H(note)))}
            </table>
            {banner}
            <div style="margin-top:24px">
              {CTA($"/exchange/cases/{c.Id}", "View Case →", accent)}
            </div>
            """);
    }
}