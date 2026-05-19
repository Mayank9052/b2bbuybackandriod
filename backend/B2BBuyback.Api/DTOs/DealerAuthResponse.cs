namespace B2BBuyback.Api.DTOs;
public class DealerAuthResponse
{
    public bool   Success      { get; set; }
    public string Token        { get; set; } = null!;   // JWT
    public string RefreshToken { get; set; } = null!;
    public int    ExpiresIn    { get; set; } = 7200;    // 2 hours in seconds
    public DealerProfileDto Dealer { get; set; } = null!;
}
