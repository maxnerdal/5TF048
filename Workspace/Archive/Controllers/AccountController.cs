using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Controllers
{
    /// AccountController handles user authentication operations, login, logout, and registration.
    /// Uses cookie-based authentication for session management.
    public class AccountController : Controller
    {
        private readonly WebApp.Services.IAuthenticationService _authService;

        public AccountController(WebApp.Services.IAuthenticationService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// GET: Account/Login
        /// Shows the login form
        /// </summary>
        /// <param name="returnUrl">URL to redirect to after successful login</param>
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // If user is already logged in, redirect to return URL or home
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToLocal(returnUrl);
            }

            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl
            };

            return View(model);
        }

        /// <summary>
        /// POST: Account/Login
        /// Processes login form submission
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            model.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validate user credentials
            var user = await _authService.ValidateUserAsync(model.Username, model.Password);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }

            // Create claims for the authenticated user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };

            // Create authentication properties
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe 
                    ? DateTimeOffset.UtcNow.AddDays(30) 
                    : DateTimeOffset.UtcNow.AddMinutes(30)
            };

            // Sign in the user
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Redirect to return URL or portfolio page
            return RedirectToLocal(returnUrl ?? Url.Action("Index", "Portfolio"));
        }

        /// <summary>
        /// GET: Account/Register
        /// Shows the registration form
        /// </summary>
        [HttpGet]
        public IActionResult Register()
        {
            // If user is already logged in, redirect to portfolio
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Portfolio");
            }

            return View(new RegisterViewModel());
        }

        /// <summary>
        /// POST: Account/Register
        /// Processes registration form submission
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if user already exists
            if (await _authService.UserExistsAsync(model.Username))
            {
                ModelState.AddModelError("Username", "Username is already taken.");
                return View(model);
            }

            // Register new user
            var user = await _authService.RegisterUserAsync(model.Username, model.Email, model.Password);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Registration failed. Email may already be in use.");
                return View(model);
            }

            // Automatically sign in the new user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            // Redirect to portfolio page
            return RedirectToAction("Index", "Portfolio");
        }

        /// <summary>
        /// POST: Account/Logout
        /// Signs out the current user
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// GET: Account/AccessDenied
        /// Shows access denied page
        /// </summary>
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        /// <summary>
        /// Helper method to safely redirect to local URLs only
        /// </summary>
        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Portfolio");
        }
    }
}
