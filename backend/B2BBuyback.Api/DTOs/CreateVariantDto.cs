namespace B2BBuyback.Api.DTOs
{
    public class CreateVariantDto
    {
        public string VariantName { get; set; } = string.Empty;
        public int ModelId { get; set; }
    }
}