using DeliveryFeeCalculator.Core.Interfaces;
using DeliveryFeeCalculator.Core.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliveryFeeCalculator.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }
        [HttpGet("login")]
        [AllowAnonymous]
        public IActionResult LoginPage()
        {
            return Content(@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset=""utf-8"">
                    <title>Login</title>
                    <style>
                        body { font-family: Arial, sans-serif; margin: 0; padding: 20px; display: flex; justify-content: center; align-items: center; height: 100vh; }
                        .login-container { width: 300px; padding: 20px; border: 1px solid #ccc; border-radius: 5px; }
                        h2 { text-align: center; }
                        .form-group { margin-bottom: 15px; }
                        label { display: block; margin-bottom: 5px; }
                        input[type='text'], input[type='password'] { width: 100%; padding: 8px; box-sizing: border-box; }
                        button { width: 100%; padding: 10px; background-color: #4CAF50; color: white; border: none; cursor: pointer; }
                        button:hover { background-color: #45a049; }
                    </style>
                </head>
                <body>
                    <div class='login-container'>
                        <h2>Login</h2>
                        <form action='/api/Auth/login' method='post'>
                            <div class='form-group'>
                                <label for='username'>Username:</label>
                                <input type='text' id='username' name='username' required>
                            </div>
                            <div class='form-group'>
                                <label for='password'>Password:</label>
                                <input type='password' id='password' name='password' required>
                            </div>
                            <button type='submit'>Login</button>
                        </form>
                    </div>
                </body>
                </html>
            ", "text/html", System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// Login to authenticate user
        /// </summary>
        /// <param name="username">User's username</param>
        /// <param name="password">User's password</param>
        /// <returns>Redirect to delivery fee calculator on success</returns>
        /// <response code="302">Redirects to calculator on successful login</response>
        /// <response code="401">If the credentials are invalid</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromForm] string username, [FromForm] string password)
        {
            _logger.LogInformation("Login attempt for user: {Username}", username);
            
            if (!_authService.ValidateUser(username, password))
            {
                _logger.LogWarning("Failed login attempt for user: {Username}", username);
                // Return back to login page with error
                return Content(@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset=""utf-8"">
                        <title>Login Failed</title>
                        <style>
                            body { font-family: Arial, sans-serif; margin: 0; padding: 20px; display: flex; justify-content: center; align-items: center; height: 100vh; }
                            .login-container { width: 300px; padding: 20px; border: 1px solid #ccc; border-radius: 5px; }
                            h2 { text-align: center; }
                            .error { color: red; margin-bottom: 15px; text-align: center; }
                            .form-group { margin-bottom: 15px; }
                            label { display: block; margin-bottom: 5px; }
                            input[type='text'], input[type='password'] { width: 100%; padding: 8px; box-sizing: border-box; }
                            button { width: 100%; padding: 10px; background-color: #4CAF50; color: white; border: none; cursor: pointer; }
                            button:hover { background-color: #45a049; }
                        </style>
                    </head>
                    <body>
                        <div class='login-container'>
                            <h2>Login</h2>
                            <div class='error'>Invalid username or password</div>
                            <form action='/api/Auth/login' method='post'>
                                <div class='form-group'>
                                    <label for='username'>Username:</label>
                                    <input type='text' id='username' name='username' required>
                                </div>
                                <div class='form-group'>
                                    <label for='password'>Password:</label>
                                    <input type='password' id='password' name='password' required>
                                </div>
                                <button type='submit'>Login</button>
                            </form>
                        </div>
                    </body>
                    </html>
                ", "text/html", System.Text.Encoding.UTF8);
            }

            // Create claims principal for user
            var principal = _authService.CreateClaimsPrincipal(username);
            
            // Sign in the user
            await HttpContext.SignInAsync("Cookies", principal);

            _logger.LogInformation("User {Username} logged in successfully", username);
            
            // Redirect to calculator page
            return Redirect("/api/DeliveryFee/calculator");
        }

        /// <summary>
        /// Logout and clear authentication
        /// </summary>
        /// <returns>Redirect to login page</returns>
        [HttpGet("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            return Redirect("/api/Auth/login");
        }

        /// <summary>
        /// Access denied page
        /// </summary>
        /// <returns>Access denied message</returns>
        [HttpGet("forbidden")]
        [AllowAnonymous]
        public IActionResult Forbidden()
        {
            return Content(@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset=""utf-8"">
                    <title>Access Denied</title>
                    <style>
                        body { font-family: Arial, sans-serif; margin: 0; padding: 20px; display: flex; justify-content: center; align-items: center; height: 100vh; }
                        .container { width: 500px; padding: 20px; border: 1px solid #ccc; border-radius: 5px; text-align: center; }
                        h2 { color: red; }
                        a { display: inline-block; margin-top: 20px; padding: 10px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px; }
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h2>Access Denied</h2>
                        <p>You do not have permission to access this page.</p>
                        <a href='/api/Auth/login'>Back to Login</a>
                    </div>
                </body>
                </html>
            ", "text/html", System.Text.Encoding.UTF8);
        }
    }
}