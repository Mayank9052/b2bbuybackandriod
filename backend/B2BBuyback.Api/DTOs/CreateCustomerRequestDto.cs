using System.ComponentModel.DataAnnotations;

public class CreateCustomerRequestDto
{
    [Required]
    [MaxLength(150)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string MobileNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    public string? State { get; set; }
    public string? Gender { get; set; }

    public string? PreferredModel { get; set; }
    public string? RequestType { get; set; }
    public string? PreferredContact { get; set; }
    public string? Notes { get; set; }
}