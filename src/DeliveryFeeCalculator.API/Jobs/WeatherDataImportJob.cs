using DeliveryFeeCalculator.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Quartz;

namespace DeliveryFeeCalculator.API.Jobs
{
    [DisallowConcurrentExecution]
    public class WeatherDataImportJob : IJob
    {
        private readonly IWeatherService _weatherService;
        private readonly ILogger<WeatherDataImportJob> _logger;

        public WeatherDataImportJob(IWeatherService weatherService, ILogger<WeatherDataImportJob> logger)
        {
            _weatherService = weatherService;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("WeatherDataImportJob started at: {time}", DateTimeOffset.Now);
            
            try
            {
                await _weatherService.ImportWeatherDataAsync();
                _logger.LogInformation("WeatherDataImportJob completed successfully at: {time}", DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing WeatherDataImportJob");
                // Re-throw the exception to let Quartz know the job failed
                throw;
            }
        }
    }
}