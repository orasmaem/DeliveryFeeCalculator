using System.Security.Claims;
using DeliveryFeeCalculator.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DeliveryFeeCalculator.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<ILogger<AuthService>> _loggerMock;
        private readonly AuthService _service;

        public AuthServiceTests()
        {
            _configurationMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<AuthService>>();
            _service = new AuthService(_configurationMock.Object, _loggerMock.Object);
        }

        [Theory]
        [InlineData("admin", "admin123", true)]
        [InlineData("user", "user123", true)]
        [InlineData("admin", "wrongpassword", false)]
        [InlineData("wronguser", "wrongpassword", false)]
        [InlineData("", "", false)]
        public void ValidateUser_ReturnsExpectedResult(string username, string password, bool expected)
        {
            // Act
            var result = _service.ValidateUser(username, password);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("admin", "Admin")]
        [InlineData("user", "User")]
        [InlineData("otheruser", "User")]
        public void CreateClaimsPrincipal_ReturnsExpectedClaims(string username, string expectedRole)
        {
            // Act
            var principal = _service.CreateClaimsPrincipal(username);

            // Assert
            Assert.NotNull(principal);
            Assert.NotNull(principal.Identity);
            Assert.True(principal.Identity.IsAuthenticated);
            Assert.Equal("DeliveryFeeCookieAuth", principal.Identity.AuthenticationType);
            
            var nameClaim = principal.FindFirst(ClaimTypes.Name);
            Assert.NotNull(nameClaim);
            Assert.Equal(username, nameClaim.Value);
            
            var roleClaim = principal.FindFirst(ClaimTypes.Role);
            Assert.NotNull(roleClaim);
            Assert.Equal(expectedRole, roleClaim.Value);
        }
    }
}