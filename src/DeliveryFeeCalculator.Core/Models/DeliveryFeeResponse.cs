namespace DeliveryFeeCalculator.Core.Models
{
    public class DeliveryFeeResponse
    {
        public decimal Fee { get; set; }
        public string? ErrorMessage { get; set; }
        
        // Detailed fee breakdown
        public decimal RegionalBaseFee { get; set; }
        public decimal ExtraFeeTemperature { get; set; }
        public decimal ExtraFeeWindSpeed { get; set; }
        public decimal ExtraFeeWeatherPhenomenon { get; set; }
        
        // Weather condition details
        public WeatherDetails? WeatherDetails { get; set; }
    }
    
    public class WeatherDetails
    {
        public string StationName { get; set; } = string.Empty;
        public decimal Temperature { get; set; }
        public decimal WindSpeed { get; set; }
        public string Phenomenon { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        
        // Fee condition flags for UI feedback
        public bool IsLowTemperature { get; set; }
        public bool IsVeryLowTemperature { get; set; }
        public bool IsHighWindSpeed { get; set; }
        public bool IsExtremeWindSpeed { get; set; }
        public bool HasSnowOrSleet { get; set; }
        public bool HasRain { get; set; }
        public bool HasDangerousPhenomenon { get; set; }
    }
}