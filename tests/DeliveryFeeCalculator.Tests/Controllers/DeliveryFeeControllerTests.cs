using DeliveryFeeCalculator.API.Controllers;
using DeliveryFeeCalculator.Core.Enums;
using DeliveryFeeCalculator.Core.Interfaces;
using DeliveryFeeCalculator.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace DeliveryFeeCalculator.Tests.Controllers
{
    public class DeliveryFeeControllerTests
    {
        private readonly Mock<IDeliveryFeeCalculationService> _serviceMock;
        private readonly Mock<ILogger<DeliveryFeeController>> _loggerMock;
        private readonly DeliveryFeeController _controller;

        public DeliveryFeeControllerTests()
        {
            _serviceMock = new Mock<IDeliveryFeeCalculationService>();
            _loggerMock = new Mock<ILogger<DeliveryFeeController>>();
            _controller = new DeliveryFeeController(_serviceMock.Object, _loggerMock.Object);
            
            // Setup mock user for controller
            var user = new ClaimsPrincipal(new ClaimsIdentity(
                new Claim[] { new Claim(ClaimTypes.Name, "testuser") }, 
                "DeliveryFeeCookieAuth"));
                
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public void CalculatorPage_ReturnsContentResult()
        {
            // Act
            var result = _controller.CalculatorPage();

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Contains("text/html", contentResult.ContentType);
            Assert.NotNull(contentResult.Content);
            Assert.Contains("<title>Delivery Fee Calculator</title>", contentResult.Content);
        }

        [Fact]
        public async Task CalculateDeliveryFee_Success_ReturnsOk()
        {
            // Arrange
            var request = new DeliveryFeeRequest { City = City.Tallinn, VehicleType = VehicleType.Car };
            var response = new DeliveryFeeResponse { 
                Fee = 4.0m,
                RegionalBaseFee = 4.0m,
                WeatherDetails = new WeatherDetails
                {
                    StationName = "Tallinn-Harku",
                    Temperature = 5.0m,
                    WindSpeed = 5.0m,
                    Phenomenon = "Clear"
                }
            };

            _serviceMock.Setup(x => x.CalculateDeliveryFeeAsync(request, false))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.CalculateDeliveryFee(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<DeliveryFeeResponse>(okResult.Value);
            Assert.Equal(4.0m, returnValue.Fee);
            Assert.NotNull(returnValue.WeatherDetails);
        }

        [Fact]
        public async Task CalculateDeliveryFee_ForbiddenVehicle_ReturnsBadRequest()
        {
            // Arrange
            var request = new DeliveryFeeRequest { City = City.Tallinn, VehicleType = VehicleType.Bike };
            var response = new DeliveryFeeResponse { 
                ErrorMessage = "Usage of selected vehicle type is forbidden: Wind speed is too high"
            };

            _serviceMock.Setup(x => x.CalculateDeliveryFeeAsync(request, false))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.CalculateDeliveryFee(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var returnValue = Assert.IsType<DeliveryFeeResponse>(badRequestResult.Value);
            Assert.Contains("forbidden", returnValue.ErrorMessage);
        }

        [Fact]
        public async Task CalculateDeliveryFee_ServiceException_ReturnsInternalServerError()
        {
            // Arrange
            var request = new DeliveryFeeRequest { City = City.Tallinn, VehicleType = VehicleType.Car };
            
            _serviceMock.Setup(x => x.CalculateDeliveryFeeAsync(request, false))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.CalculateDeliveryFee(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var returnValue = Assert.IsType<DeliveryFeeResponse>(statusCodeResult.Value);
            Assert.Contains("error occurred", returnValue.ErrorMessage);
        }
    }
}