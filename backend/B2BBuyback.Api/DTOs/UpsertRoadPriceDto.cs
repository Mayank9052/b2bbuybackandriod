namespace B2BBuyback.Api.DTOs
{

    public class UpsertRoadPriceDto
    {
        public int ScootyId { get; set; }
        public int CityId { get; set; }
        public decimal ExShowroomPrice { get; set; }
        public decimal RtoCharges { get; set; }
        public decimal InsuranceAmount { get; set; }
        public decimal OtherCharges { get; set; }
        public DateOnly? ValidFrom { get; set; }
    }
}