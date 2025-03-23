using System.Security.Claims;

namespace DeliveryFeeCalculator.Core.Interfaces
{
    /// <summary>
    /// Service for user authentication and claims management
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Validates user credentials against the stored user list
        /// </summary>
        /// <param name="username">The username to validate</param>
        /// <param name="password">The password to validate</param>
        /// <returns>True if credentials are valid, false otherwise</returns>
        bool ValidateUser(string username, string password);

        /// <summary>
        /// Creates a claims principal for the authenticated user
        /// </summary>
        /// <param name="username">The username to create claims for</param>
        /// <returns>A ClaimsPrincipal containing the user's identity and claims</returns>
        ClaimsPrincipal CreateClaimsPrincipal(string username);
    }
}