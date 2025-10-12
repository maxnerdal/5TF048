namespace WebApp.Models
{
    /// <summary>
    /// SIMPLE USER MODEL for Assignment
    /// Basic user representation without Entity Framework complexity
    /// This is your data container - holds user information
    /// </summary>
    public class SimpleUser
    {
        /// <summary>
        /// User ID - Primary key from database
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Username for login
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// User's email address
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Hashed password (never store plain text!)
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// When the user was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}