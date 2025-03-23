using DeliveryFeeCalculator.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DeliveryFeeCalculator.Infrastructure.Data
{
    public class WeatherDbContext : DbContext
    {
        public WeatherDbContext(DbContextOptions<WeatherDbContext> options) : base(options)
        {
        }

        public DbSet<WeatherData> WeatherData { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Weather data configuration
            modelBuilder.Entity<WeatherData>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.StationName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.WmoCode).IsRequired().HasMaxLength(20);
                entity.Property(e => e.AirTemperature).HasPrecision(5, 2);
                entity.Property(e => e.WindSpeed).HasPrecision(5, 2);
                entity.Property(e => e.WeatherPhenomenon).HasMaxLength(255);
                entity.Property(e => e.Timestamp).IsRequired();
                
                // Create an index on StationName and Timestamp for faster queries
                entity.HasIndex(e => new { e.StationName, e.Timestamp });
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}