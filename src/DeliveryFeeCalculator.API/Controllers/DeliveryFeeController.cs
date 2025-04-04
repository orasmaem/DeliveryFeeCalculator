using DeliveryFeeCalculator.Core.Enums;
using DeliveryFeeCalculator.Core.Interfaces;
using DeliveryFeeCalculator.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace DeliveryFeeCalculator.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class DeliveryFeeController : ControllerBase
    {
        private readonly IDeliveryFeeCalculationService _deliveryFeeCalculationService;
        private readonly ILogger<DeliveryFeeController> _logger;

        public DeliveryFeeController(
            IDeliveryFeeCalculationService deliveryFeeCalculationService,
            ILogger<DeliveryFeeController> logger)
        {
            _deliveryFeeCalculationService = deliveryFeeCalculationService;
            _logger = logger;
        }

        /// <summary>
        /// Web-based calculator page
        /// </summary>
        /// <returns>HTML page with calculator form</returns>
        [HttpGet("calculator")]
        [Authorize]
        public IActionResult CalculatorPage()
        {
            var username = User.Identity?.Name ?? "User";

            var htmlBuilder = new StringBuilder();
            htmlBuilder.Append(@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset=""utf-8"">
                    <title>Delivery Fee Calculator</title>
                    <style>
                        body { font-family: Arial, sans-serif; margin: 0; padding: 20px; }
                        .container { max-width: 800px; margin: 0 auto; padding: 20px; border: 1px solid #ccc; border-radius: 5px; }
                        h1, h2 { text-align: center; }
                        h3 { margin-top: 20px; font-size: 16px; color: #333; }
                        .user-info { text-align: right; margin-bottom: 20px; }
                        .form-group { margin-bottom: 15px; }
                        label { display: block; margin-bottom: 5px; }
                        select { width: 100%; padding: 8px; box-sizing: border-box; }
                        button { width: 100%; padding: 10px; background-color: #4CAF50; color: white; border: none; cursor: pointer; margin-top: 10px; }
                        button:hover { background-color: #45a049; }
                        .result { margin-top: 20px; padding: 15px; border: 1px solid #ddd; border-radius: 5px; display: none; }
                        .result.success { background-color: #f0f8ff; border-color: #b8daff; }
                        .result.error { background-color: #f8d7da; border-color: #f5c6cb; }
                        .fee { font-size: 24px; font-weight: bold; text-align: center; margin: 10px 0; }
                        .error-message { color: red; font-weight: bold; text-align: center; }
                        .logout { display: inline-block; padding: 5px 10px; background-color: #f44336; color: white; text-decoration: none; border-radius: 3px; }
                        .test-data { margin-top: 10px; }
                        .weather-info { margin-top: 20px; font-size: 14px; color: #333; }
                        .weather-info p { margin: 5px 0; }
                        .weather-info ul { padding-left: 20px; margin: 10px 0; }
                        .weather-info li { margin-bottom: 5px; }
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='user-info'>
                            Logged in as: <strong>").Append(username).Append(@"</strong> | 
                            <a href='/api/Auth/logout' class='logout'>Logout</a>
                        </div>
                        <h1>Delivery Fee Calculator</h1>
                        <p>Calculate delivery fees based on city, vehicle type, and current weather conditions.</p>
                        
                        <form id='calculatorForm'>
                            <div class='form-group'>
                                <label for='city'>City:</label>
                                <select id='city' name='city'>
                                    <option value='Tallinn'>Tallinn</option>
                                    <option value='Tartu'>Tartu</option>
                                    <option value='Pärnu'>Pärnu</option>
                                </select>
                            </div>
                            <div class='form-group'>
                                <label for='vehicleType'>Vehicle Type:</label>
                                <select id='vehicleType' name='vehicleType'>
                                    <option value='Car'>Car</option>
                                    <option value='Scooter'>Scooter</option>
                                    <option value='Bike'>Bike</option>
                                </select>
                            </div>
                            <div class='form-group test-data'>
                                <label for='useTestData'>
                                    <input type='checkbox' id='useTestData' name='useTestData'> 
                                    Use test data (extreme weather conditions)
                                </label>
                            </div>
                            <button type='submit'>Calculate Delivery Fee</button>
                        </form>
                        
                        <div id='resultSuccess' class='result success'>
                            <h2>Delivery Fee</h2>
                            <div id='feeAmount' class='fee'>€0.00</div>
                            <div class='weather-info'>
                                <strong>Weather conditions:</strong>
                                <div id='weatherDetails'></div>
                            </div>
                        </div>
                        
                        <div id='resultError' class='result error'>
                            <h2>Cannot Calculate Fee</h2>
                            <div id='errorMessage' class='error-message'></div>
                        </div>
                    </div>

                    <script>
                        document.getElementById('calculatorForm').addEventListener('submit', function(e) {
                            e.preventDefault();
                            
                            const city = document.getElementById('city').value;
                            const vehicleType = document.getElementById('vehicleType').value;
                            const useTestData = document.getElementById('useTestData').checked;
                            
                            // Hide result divs
                            document.getElementById('resultSuccess').style.display = 'none';
                            document.getElementById('resultError').style.display = 'none';
                            
                            // Make API request
                            fetch(`/api/DeliveryFee/calculate?city=${city}&vehicleType=${vehicleType}&useTestData=${useTestData}`)
                                .then(response => response.json())
                                .then(data => {
                                    if (data.errorMessage && data.errorMessage.includes('forbidden')) {
                                        // Show error result
                                        document.getElementById('errorMessage').textContent = data.errorMessage;
                                        document.getElementById('resultError').style.display = 'block';
                                    } else {
                                        // Show success result
                                        document.getElementById('feeAmount').textContent = '€' + data.fee.toFixed(2);
                                        
                                        // Display weather details and condition flags
                                        const weatherDetailsElement = document.getElementById('weatherDetails');
                                        weatherDetailsElement.innerHTML = '';
                                        
                                        if (data.errorMessage) {
                                            weatherDetailsElement.textContent = data.errorMessage;
                                        } 
                                        else if (data.weatherDetails) {
                                            const wd = data.weatherDetails;
                                            
                                            // Create HTML for weather conditions
                                            const weatherInfo = document.createElement('div');
                                            
                                            // Add basic weather data
                                            weatherInfo.innerHTML = `
                                                <p><strong>Station:</strong> ${wd.stationName}</p>
                                                <p><strong>Temperature:</strong> ${wd.temperature.toFixed(1)}°C</p>
                                                <p><strong>Wind Speed:</strong> ${wd.windSpeed.toFixed(1)} m/s</p>
                                                <p><strong>Weather Phenomenon:</strong> ${wd.phenomenon || 'None'}</p>
                                                <p><strong>Timestamp:</strong> ${new Date(wd.timestamp).toLocaleString()}</p>
                                            `;
                                            
                                            // Add fee breakdown
                                            const feeBreakdown = document.createElement('div');
                                            feeBreakdown.innerHTML = `
                                                <h3>Fee Breakdown:</h3>
                                                <ul>
                                                    <li>Regional Base Fee: €${data.regionalBaseFee.toFixed(2)}</li>
                                                    ${data.extraFeeTemperature > 0 ? `<li>Temperature Extra Fee: €${data.extraFeeTemperature.toFixed(2)}</li>` : ''}
                                                    ${data.extraFeeWindSpeed > 0 ? `<li>Wind Speed Extra Fee: €${data.extraFeeWindSpeed.toFixed(2)}</li>` : ''}
                                                    ${data.extraFeeWeatherPhenomenon > 0 ? `<li>Weather Phenomenon Extra Fee: €${data.extraFeeWeatherPhenomenon.toFixed(2)}</li>` : ''}
                                                </ul>
                                                <p><strong>Total Fee:</strong> €${data.fee.toFixed(2)}</p>
                                            `;
                                            
                                            // Add weather condition explanations if any conditions are flagged
                                            if (wd.isLowTemperature || wd.isVeryLowTemperature || wd.isHighWindSpeed || 
                                                wd.isExtremeWindSpeed || wd.hasSnowOrSleet || wd.hasRain || wd.hasDangerousPhenomenon) {
                                                
                                                const conditionExplanations = document.createElement('div');
                                                conditionExplanations.innerHTML = '<h3>Weather Conditions Affecting Fee:</h3><ul>';
                                                
                                                if (wd.isLowTemperature) {
                                                    conditionExplanations.innerHTML += '<li>Cold temperature (0°C to -10°C): Extra fee applied for bike/scooter</li>';
                                                }
                                                
                                                if (wd.isVeryLowTemperature) {
                                                    conditionExplanations.innerHTML += '<li>Very cold temperature (below -10°C): Higher extra fee applied for bike/scooter</li>';
                                                }
                                                
                                                if (wd.isHighWindSpeed) {
                                                    conditionExplanations.innerHTML += '<li>High wind speed (10-20 m/s): Extra fee applied for bike</li>';
                                                }
                                                
                                                if (wd.isExtremeWindSpeed) {
                                                    conditionExplanations.innerHTML += '<li>Extreme wind speed (above 20 m/s): Bike usage prohibited</li>';
                                                }
                                                
                                                if (wd.hasSnowOrSleet) {
                                                    conditionExplanations.innerHTML += '<li>Snow or sleet: Extra fee applied for bike/scooter</li>';
                                                }
                                                
                                                if (wd.hasRain) {
                                                    conditionExplanations.innerHTML += '<li>Rain: Extra fee applied for bike/scooter</li>';
                                                }
                                                
                                                if (wd.hasDangerousPhenomenon) {
                                                    conditionExplanations.innerHTML += '<li>Dangerous weather phenomenon (glaze, hail, or thunder): Bike/scooter usage prohibited</li>';
                                                }
                                                
                                                conditionExplanations.innerHTML += '</ul>';
                                                weatherInfo.appendChild(conditionExplanations);
                                            }
                                            
                                            weatherInfo.appendChild(feeBreakdown);
                                            weatherDetailsElement.appendChild(weatherInfo);
                                        } 
                                        else {
                                            weatherDetailsElement.textContent = 'Weather data is reflected in the calculated fee.';
                                        }
                                        
                                        document.getElementById('resultSuccess').style.display = 'block';
                                    }
                                })
                                .catch(error => {
                                    document.getElementById('errorMessage').textContent = 
                                        'An error occurred while calculating the delivery fee. Please try again.';
                                    document.getElementById('resultError').style.display = 'block';
                                });
                        });
                    </script>
                </body>
                </html>
            ");

            return Content(htmlBuilder.ToString(), "text/html", System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// Calculates the delivery fee based on city and vehicle type
        /// </summary>
        /// <param name="request">The delivery fee request containing city and vehicle type</param>
        /// <param name="useTestData">Optional: Set to true to use test weather data with extreme conditions</param>
        /// <returns>The calculated delivery fee or an error message</returns>
        /// <response code="200">Returns the calculated delivery fee</response>
        /// <response code="400">If the vehicle usage is forbidden due to weather conditions</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpGet("calculate")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DeliveryFeeResponse>> CalculateDeliveryFee(
            [FromQuery] DeliveryFeeRequest request,
            [FromQuery] bool useTestData = false)
        {
            try
            {
                _logger.LogInformation("Calculating delivery fee for {City} with {VehicleType}. UseTestData: {UseTestData}", 
                    request.City, request.VehicleType, useTestData);
                
                var response = await _deliveryFeeCalculationService.CalculateDeliveryFeeAsync(request, useTestData);
                
                if (!string.IsNullOrEmpty(response.ErrorMessage) && response.ErrorMessage.Contains("forbidden"))
                {
                    _logger.LogWarning("Vehicle usage forbidden: {ErrorMessage}", response.ErrorMessage);
                    return BadRequest(response);
                }
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating delivery fee");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new DeliveryFeeResponse { ErrorMessage = "An error occurred while calculating the delivery fee." });
            }
        }
    }
}