using DeliveryFeeCalculator.Core.Enums;
using DeliveryFeeCalculator.Core.Interfaces;
using DeliveryFeeCalculator.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace DeliveryFeeCalculator.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
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
        /// Calculates the delivery fee based on city and vehicle type
        /// </summary>
        /// <param name="request">The delivery fee request containing city and vehicle type</param>
        /// <param name="useTestData">Optional: Set to true to use test weather data with extreme conditions</param>
        /// <returns>The calculated delivery fee or an error message</returns>
        /// <response code="200">Returns the calculated delivery fee</response>
        /// <response code="400">If the vehicle usage is forbidden due to weather conditions</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpGet("calculate")]
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