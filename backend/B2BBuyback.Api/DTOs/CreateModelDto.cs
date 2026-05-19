namespace B2BBuyback.Api.DTOs
{
    public class CreateModelDto
    {
        public string ModelName { get; set; } = string.Empty;
    }

    public class UpdateModelDto
    {
        public int Id { get; set; }
        public string ModelName { get; set; } = string.Empty;
    }
}