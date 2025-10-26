using WebApp.Models;

namespace WebApp.Services
{
    /// <summary>
    /// Interface for authentication operations
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Validates user credentials and returns the user if valid
        /// </summary>
        /// <param name="username">Username to validate</param>
        /// <param name="password">Plain text password</param>
        /// <returns>User object if credentials are valid, null otherwise</returns>
        Task<User?> ValidateUserAsync(string username, string password);

        /// <summary>
        /// Registers a new user
        /// </summary>
        /// <param name="username">Unique username</param>
        /// <param name="email">User's email address</param>
        /// <param name="password">Plain text password (will be hashed)</param>
        /// <returns>Created user if successful, null if username/email already exists</returns>
        Task<User?> RegisterUserAsync(string username, string email, string password);

        /// <summary>
        /// Checks if a username already exists
        /// </summary>
        /// <param name="username">Username to check</param>
        /// <returns>True if username exists, false otherwise</returns>
        Task<bool> UserExistsAsync(string username);

        /// <summary>
        /// Gets a user by their ID
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>User if found, null otherwise</returns>
        Task<User?> GetUserByIdAsync(int userId);

        /// <summary>
        /// Gets a user by their username
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>User if found, null otherwise</returns>
        Task<User?> GetUserByUsernameAsync(string username);

        /// <summary>
        /// Updates a user's password
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="newPassword">New plain text password (will be hashed)</param>
        /// <returns>True if password was updated successfully, false otherwise</returns>
        Task<bool> UpdatePasswordAsync(int userId, string newPassword);

        /// <summary>
        /// Gets the currently authenticated user from the HTTP context
        /// </summary>
        /// <param name="httpContext">HTTP context containing user claims</param>
        /// <returns>Current user if authenticated, null otherwise</returns>
        Task<User?> GetCurrentUserAsync(Microsoft.AspNetCore.Http.HttpContext httpContext);
    }
}
