// Interfaces/IExchangeEmailService.cs
// UPDATED: Both methods now accept User object instead of plain email string.
// This allows email templates to greet the dealer by FullName.

using B2BBuyback.Api.Models;

namespace B2BBuyback.Api.Interfaces;

public interface IExchangeEmailService
{
    /// <summary>
    /// Sends dealer ACK + admin notification when a case is submitted.
    /// dealer.Email = destination for dealer ACK
    /// AdminEmail   = destination for admin notification (from appsettings)
    /// </summary>
    Task SendCaseSubmissionEmailsAsync(ExchangeCase c, User dealer);

    /// <summary>
    /// Sends decision email (Approved / Modified / Rejected) to dealer.
    /// dealer.Email = destination
    /// </summary>
    Task SendAdminActionEmailAsync(ExchangeCase c, User dealer, string action, string? note);
}