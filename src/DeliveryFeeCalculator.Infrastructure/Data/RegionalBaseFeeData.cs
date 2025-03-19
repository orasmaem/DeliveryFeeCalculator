using DeliveryFeeCalculator.Core.Enums;

namespace DeliveryFeeCalculator.Infrastructure.Data
{
    public static class RegionalBaseFeeData
    {
        // Dictionary of base fees by city and vehicle type
        private static readonly Dictionary<(City, VehicleType), decimal> BaseFees = new()
        {
            // Tallinn
            { (City.Tallinn, VehicleType.Car), 4.0m },
            { (City.Tallinn, VehicleType.Scooter), 3.5m },
            { (City.Tallinn, VehicleType.Bike), 3.0m },
            
            // Tartu
            { (City.Tartu, VehicleType.Car), 3.5m },
            { (City.Tartu, VehicleType.Scooter), 3.0m },
            { (City.Tartu, VehicleType.Bike), 2.5m },
            
            // Pärnu
            { (City.Pärnu, VehicleType.Car), 3.0m },
            { (City.Pärnu, VehicleType.Scooter), 2.5m },
            { (City.Pärnu, VehicleType.Bike), 2.0m }
        };

        public static decimal GetBaseFee(City city, VehicleType vehicleType)
        {
            if (BaseFees.TryGetValue((city, vehicleType), out decimal fee))
            {
                return fee;
            }

            throw new ArgumentException($"No base fee defined for city {city} and vehicle type {vehicleType}");
        }
    }
}