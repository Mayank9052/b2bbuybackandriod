namespace B2BBuyback.Api.DTOs;
public class ScootySpecDto
{
    public string? FuelType { get; set; }
    public bool ReverseMode { get; set; }
    public bool CruiseControl { get; set; }
    public bool UsbCharging { get; set; }
    public string? RidingModes { get; set; }
    public string? WaterWading { get; set; }
    public int? GroundClearance { get; set; }
    public int? VehicleWeight { get; set; }
    public string? BatteryWarranty { get; set; }
    public string? MotorWarranty { get; set; }
}