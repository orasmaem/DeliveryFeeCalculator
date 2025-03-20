namespace DeliveryFeeCalculator.Core.Constants
{
    public static class WeatherStations
    {
        // Exact names as they appear in the XML
        public const string TallinnHarku = "Tallinn-Harku";
        public const string TartuToravere = "Tartu-Tõravere";
        public const string Parnu = "Pärnu";
        
        // Test station names
        public const string TallinnHarkuTest = "Tallinn-Harku-Test";
        public const string TartuToravereTest = "Tartu-Tõravere-Test";
        public const string ParnuTest = "Pärnu-Test";
        
        // WMO codes for the stations
        public const string TallinnHarkuWmoCode = "26038";
        public const string TartuToravereWmoCode = "26242";
        public const string ParnuWmoCode = "41803";
        
        // Dictionary mapping cities to stations
        public static class CityToStation
        {
            // Use this for production
            public static readonly Dictionary<Core.Enums.City, string> Map = new()
            {
                { Core.Enums.City.Tallinn, TallinnHarku },
                { Core.Enums.City.Tartu, TartuToravere },
                { Core.Enums.City.Pärnu, Parnu }
            };
            
            // Use this for testing with extreme weather data
            public static readonly Dictionary<Core.Enums.City, string> TestMap = new()
            {
                { Core.Enums.City.Tallinn, TallinnHarkuTest },
                { Core.Enums.City.Tartu, TartuToravereTest },
                { Core.Enums.City.Pärnu, ParnuTest }
            };
        }
    }
}