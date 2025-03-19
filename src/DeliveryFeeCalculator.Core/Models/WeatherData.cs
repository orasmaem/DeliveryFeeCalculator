namespace DeliveryFeeCalculator.Core.Models
{
    public class WeatherData
    {
        public int Id { get; set; }
        public string StationName { get; set; } = string.Empty;
        public string WmoCode { get; set; } = string.Empty;
        public decimal AirTemperature { get; set; }
        public decimal WindSpeed { get; set; }
        public string WeatherPhenomenon { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}