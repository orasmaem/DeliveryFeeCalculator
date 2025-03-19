namespace DeliveryFeeCalculator.Core.Models
{
    public class DeliveryFeeResponse
    {
        public decimal Fee { get; set; }
        public string? ErrorMessage { get; set; }
    }
}