namespace B2BBuyback.Api.DTOs;

public class StartCaseDto
    {
        public string CustomerName   { get; set; } = string.Empty;
        public string MobileNumber   { get; set; } = string.Empty;
        public string City           { get; set; } = string.Empty;
        public string VehicleModel   { get; set; } = string.Empty;
        public string RegistrationNo { get; set; } = string.Empty;
        public int    YearOfPurchase { get; set; }
        public int    KmDriven       { get; set; }
        public string? VehicleVariant { get; set; }
    }