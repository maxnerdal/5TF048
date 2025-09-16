using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Services
{
    /// <summary>
    /// Authentication service with database storage using Entity Framework Core.
    /// This replaces the previous in-memory implementation with persistent database storage.
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Constructor with dependency injection for database context
        /// </summary>
        /// <param name="context">Database context for user operations</param>
        public AuthenticationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> ValidateUserAsync(string username, string password)
        {
            // Find user by username (case-insensitive) from database
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

            if (user == null)
                return null;

            // Verify password against stored hash
            if (VerifyPassword(password, user.PasswordHash))
                return user;

            return null;
        }

        public async Task<User?> RegisterUserAsync(string username, string email, string password)
        {
            // Check if username already exists in database
            if (await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower()))
                return null;

            // Check if email already exists in database
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower()))
                return null;

            // Create new user entity
            var newUser = new User
            {
                Username = username,
                Email = email,
                PasswordHash = HashPassword(password),
                CreatedAt = DateTime.UtcNow
            };

            // Add user to database
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return newUser;
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
        /// Verifies a password against its hash
        /// </summary>
        private static bool VerifyPassword(string password, string hash)
        {
            var computedHash = HashPassword(password);
            return computedHash == hash;
        }
    }
}
