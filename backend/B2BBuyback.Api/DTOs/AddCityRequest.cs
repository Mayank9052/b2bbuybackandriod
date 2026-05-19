namespace B2BBuyback.Api.DTOs;

public class AddCityRequest
{
    public string CityName { get; set; } = "";
    public string? StateName { get; set; }
    public bool IsPopular { get; set; }
    public string? PincodePrefix { get; set; }
    public List<AddAreaRequest>? Areas { get; set; }
}