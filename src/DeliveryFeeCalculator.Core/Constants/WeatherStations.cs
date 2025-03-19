namespace DeliveryFeeCalculator.Core.Constants
{
    public static class WeatherStations
    {
        // Exact names as they appear in the XML
        public const string TallinnHarku = "Tallinn-Harku";
        public const string TartuToravere = "Tartu-Tõravere";
        public const string Parnu = "Pärnu";
        
        // WMO codes for the stations
        public const string TallinnHarkuWmoCode = "26038";
        public const string TartuToravereWmoCode = "26242";
        public const string ParnuWmoCode = "41803";
        
        // Dictionary mapping cities to stations
        public static class CityToStation
        {
            public static readonly Dictionary<Core.Enums.City, string> Map = new()
            {
                { Core.Enums.City.Tallinn, TallinnHarku },
                { Core.Enums.City.Tartu, TartuToravere },
                { Core.Enums.City.Pärnu, Parnu }
            };
        }
    }
}