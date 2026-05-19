namespace B2BBuyback.Api.DTOs
{
    public class UpdateVariantDto
    {
        public int Id { get; set; }
        public string VariantName { get; set; } = string.Empty;
        public int ModelId { get; set; }
    }
}