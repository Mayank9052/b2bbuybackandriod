namespace B2BBuyback.Api.DTOs;

public class CreateComparisonConfigDto
{
    public int  Scooty1Id { get; set; }
    public int  Scooty2Id { get; set; }
    public int? Scooty3Id { get; set; }   // nullable — null for 2-bike comparison
}