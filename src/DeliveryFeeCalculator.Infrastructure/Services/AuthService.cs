using System.Security.Claims;
using DeliveryFeeCalculator.Core.Models;
using Microsoft.Extensions.Configuration;

namespace DeliveryFeeCalculator.Infrastructure.Services
{
    public class AuthService
    {
        private readonly IConfiguration _configuration;

        // In a real application, these would be stored securely, possibly in a database
        private readonly List<UserCredentials> _users = new()
        {
            new UserCredentials { Username = "admin", Password = "admin123" },
            new UserCredentials { Username = "user", Password = "user123" }
        };

        public AuthService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool ValidateUser(string username, string password)
        {
            // Find the user
            var user = _users.FirstOrDefault(u => 
                u.Username == username && 
                u.Password == password);

            return user != null;
        }

        public ClaimsPrincipal CreateClaimsPrincipal(string username)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, username == "admin" ? "Admin" : "User")
            };

            var identity = new ClaimsIdentity(claims, "DeliveryFeeCookieAuth");
            return new ClaimsPrincipal(identity);
        }
    }
}