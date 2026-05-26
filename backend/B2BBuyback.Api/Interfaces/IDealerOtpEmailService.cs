// B2BBuyback.Api/Interfaces/IDealerOtpEmailService.cs
namespace B2BBuyback.Api.Interfaces
{
    public interface IDealerOtpEmailService
    {
        Task SendOtpEmailAsync(
            string toEmail,
            string dealerName,
            string otpCode,
            int expiryMinutes = 5);
    }
}