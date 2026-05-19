namespace B2BBuyback.Api.DTOs
{
    public class CreateColourDto
    {
        public string ColourName { get; set; } = string.Empty;
        public int ModelId { get; set; }
        public int VariantId { get; set; }
        //public string? HexCode    { get; set; } 
    }

    public class UpdateColourDto
    {
        public int Id { get; set; }
        public string ColourName { get; set; } = string.Empty;
        public int ModelId { get; set; }
        public int VariantId { get; set; }
        //public string? HexCode    { get; set; } 
    }
}