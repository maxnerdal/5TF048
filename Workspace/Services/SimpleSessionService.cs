using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;

namespace WebApp.Services
{
    public class SimpleSessionService : ISimpleSessionService
    {
        private readonly ConcurrentDictionary<string, Dictionary<string, object>> _sessions;

        public SimpleSessionService()
        {
            _sessions = new ConcurrentDictionary<string, Dictionary<string, object>>();
        }

        private string GetSessionId(HttpContext context)
        {
            const string cookieName = "SimpleSessionId";
            
            // Try to get existing session ID from cookie
            if (context.Request.Cookies.TryGetValue(cookieName, out var sessionId) && !string.IsNullOrEmpty(sessionId))
            {
                return sessionId;
            }

            // Create new session ID
            sessionId = Guid.NewGuid().ToString();
            
            // Set cookie with session ID
            context.Response.Cookies.Append(cookieName, sessionId, new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // Allow HTTP in development
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddMinutes(30)
            });

            return sessionId;
        }

        private Dictionary<string, object> GetSessionData(string sessionId)
        {
            return _sessions.GetOrAdd(sessionId, _ => new Dictionary<string, object>());
        }

        public void SetString(HttpContext context, string key, string value)
        {
            var sessionId = GetSessionId(context);
            var sessionData = GetSessionData(sessionId);
            sessionData[key] = value;
        }

        public string? GetString(HttpContext context, string key)
        {
            var sessionId = GetSessionId(context);
            var sessionData = GetSessionData(sessionId);
            return sessionData.TryGetValue(key, out var value) ? value?.ToString() : null;
        }

        public void SetInt(HttpContext context, string key, int value)
        {
            var sessionId = GetSessionId(context);
            var sessionData = GetSessionData(sessionId);
            sessionData[key] = value;
        }

        public int GetInt(HttpContext context, string key)
        {
            var sessionId = GetSessionId(context);
            var sessionData = GetSessionData(sessionId);
            if (sessionData.TryGetValue(key, out var value) && value is int intValue)
            {
                return intValue;
            }
            return 0;
        }

        public void Clear(HttpContext context)
        {
            var sessionId = GetSessionId(context);
            _sessions.TryRemove(sessionId, out _);
        }
    }
}
