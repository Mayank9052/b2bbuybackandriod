// Add this class to your existing DTOs file (e.g. DTOs/DealerDTOs.cs or a shared DTOs.cs)
// This fixes: EmiEnquiryController.UpdateStatus([FromBody] string) → Swagger crash

namespace B2BBuyback.Api.DTOs;

/// <summary>
/// Simple wrapper so Swagger can generate a proper JSON schema for status updates.
/// Replaces the bare [FromBody] string that caused the swagger/v1/swagger.json 500 error.
/// </summary>
public class UpdateStatusDto
{
    public string Status { get; set; } = null!;
}
