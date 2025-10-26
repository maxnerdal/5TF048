using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Services
{
    /// *** AUTHENTICATION DAL SERVICE ***
    /// This service handles all user authentication database operations.
    /// It demonstrates how DAL separates security logic from database access.
    /// 
    /// DAL Benefits here:
    /// - Centralized user database operations
    /// - Testable authentication logic
    /// - Separation between authentication logic and database code
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        // *** DAL DEPENDENCY *** - Database context for user operations
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// *** DEPENDENCY INJECTION *** 
        /// Constructor receives database context - this is the DAL pattern in action
        /// The service doesn't create its own database connection, it receives one
        /// </summary>
        /// <param name="context">Database context for user operations</param>
        public AuthenticationService(ApplicationDbContext context)
        {
            _context = context; // Store the DAL reference
        }

        public async Task<User?> ValidateUserAsync(string username, string password)
        {
            // *** DATABASE QUERY *** - Use DAL to find user in database
            // This demonstrates READ operation through Entity Framework
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

            if (user == null)
                return null; // User not found in database

            // *** BUSINESS LOGIC *** - Verify password (not database operation)
            // Notice how DAL separates database access from business logic
            if (VerifyPassword(password, user.PasswordHash))
                return user; // Authentication successful

            return null; // Password incorrect
        }

        public async Task<User?> RegisterUserAsync(string username, string email, string password)
        {
            // *** VALIDATION QUERIES *** - Check for duplicates before creating
            // These are READ operations to ensure data integrity
            if (await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower()))
                return null; // Username already exists

            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower()))
                return null; // Email already exists

            // *** ENTITY CREATION *** - Prepare new user for database
            var newUser = new User
            {
                Username = username,
                Email = email,
                PasswordHash = HashPassword(password), // Business logic: hash the password
                CreatedAt = DateTime.UtcNow             // Set audit timestamp
            };

            // *** CREATE OPERATION *** - Add to database through DAL
            _context.Users.Add(newUser);              // Mark for insertion
            await _context.SaveChangesAsync();        // Execute INSERT statement

            return newUser; // Return the created user (now has database-generated ID)
        }

        public async Task<bool> UserExistsAsync(string username)
        {
            return await _context.Users
                .AnyAsync(u => u.Username.ToLower() == username.ToLower());
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        }

        public async Task<bool> UpdatePasswordAsync(int userId, string newPassword)
        {
            // *** READ OPERATION *** - Find the user to update
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            
            if (user == null)
                return false; // User not found

            // *** UPDATE OPERATION *** - Modify user's password hash
            user.PasswordHash = HashPassword(newPassword);
            
            // *** SAVE CHANGES *** - Persist the update to database
            try
            {
                await _context.SaveChangesAsync();
                return true; // Password updated successfully
            }
            catch
            {
                return false; // Update failed
            }
        }

        /// <summary>
        /// Hashes a password using SHA256 with salt. 
        /// NOTE: In production, use bcrypt, scrypt, or Argon2 for better security.
        /// </summary>
        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "YourSaltHere"));
            return Convert.ToBase64String(hashedBytes);
        }

        /// <summary>
        /// Gets the currently authenticated user from the HTTP context
        /// </summary>
        public async Task<User?> GetCurrentUserAsync(Microsoft.AspNetCore.Http.HttpContext httpContext)
        {
            if (httpContext.User?.Identity?.IsAuthenticated != true)
                return null;

            var username = httpContext.User.Identity.Name;
            if (string.IsNullOrEmpty(username))
                return null;

            return await GetUserByUsernameAsync(username);
        }

        /// <summary>
        /// Verifies a password against its hash
        /// </summary>
        private static bool VerifyPassword(string password, string hash)
        {
            var computedHash = HashPassword(password);
            return computedHash == hash;
        }
    }
}
