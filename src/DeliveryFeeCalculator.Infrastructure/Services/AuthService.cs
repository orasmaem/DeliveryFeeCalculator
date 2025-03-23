using System.Security.Claims;
using DeliveryFeeCalculator.Core.Interfaces;
using DeliveryFeeCalculator.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DeliveryFeeCalculator.Infrastructure.Services
{
    /// <summary>
    /// Implementation of IAuthService for user authentication and claims management
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        // In a real application, these would be stored securely, possibly in a database
        private readonly List<UserCredentials> _users = new()
        {
            new UserCredentials { Username = "admin", Password = "admin123" },
            new UserCredentials { Username = "user", Password = "user123" }
        };

        /// <summary>
        /// Initializes a new instance of the AuthService class
        /// </summary>
        /// <param name="configuration">Application configuration</param>
        /// <param name="logger">Logger for the AuthService</param>
        public AuthService(IConfiguration configuration, ILogger<AuthService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <inheritdoc />
        public bool ValidateUser(string username, string password)
        {
            try
            {
                // Find the user
                var user = _users.FirstOrDefault(u => 
                    u.Username == username && 
                    u.Password == password);

                if (user == null)
                {
                    _logger.LogWarning("Failed login attempt for username: {Username}", username);
                    return false;
                }

                _logger.LogInformation("User {Username} logged in successfully", username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user {Username}", username);
                return false;
            }
        }

        /// <inheritdoc />
        public ClaimsPrincipal CreateClaimsPrincipal(string username)
        {
            try
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, username == "admin" ? "Admin" : "User")
                };

                var identity = new ClaimsIdentity(claims, "DeliveryFeeCookieAuth");
                return new ClaimsPrincipal(identity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating claims principal for {Username}", username);
                throw;
            }
        }
    }
}