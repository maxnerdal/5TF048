using Microsoft.AspNetCore.Http;

namespace WebApp.Services
{
    public interface ISimpleSessionService
    {
        void SetString(HttpContext context, string key, string value);
        string? GetString(HttpContext context, string key);
        void SetInt(HttpContext context, string key, int value);
        int GetInt(HttpContext context, string key);
        void Clear(HttpContext context);
    }
}
