using WebApp.Services;
using WebApp.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Configure culture to use en-US for consistent decimal parsing (fixes Swedish locale issue)
var supportedCultures = new[] { new CultureInfo("en-US") };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Temporarily disable antiforgery validation for development
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryTokenAttribute());
});

// Get connection string from configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Register Entity Framework for Portfolio and BTC functionality
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register HttpClient for BTC price service
builder.Services.AddHttpClient();

// Register portfolio and BTC price services (existing functionality)
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IBtcPriceService, BtcPriceService>();

// Register the Entity Framework-based authentication service (for AccountController)
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Register bot configuration service for JSON parameter management
builder.Services.AddScoped<IBotConfigurationService, BotConfigurationService>();

// Register trading bot and backtesting services (adapted for JSON configuration)
builder.Services.AddScoped<ITradingBotDAL, TradingBotDAL>();
builder.Services.AddScoped<ITradingBotService, TradingBotService>();
builder.Services.AddScoped<IBacktestingService, BacktestingService>();

// TODO: Add back when needed for strategy execution
// builder.Services.AddScoped<IBotStrategyFactory, BotStrategyFactory>();

// Register background service for daily market data updates
// DISABLED: Uncomment the line below to enable automatic daily updates
// builder.Services.AddHostedService<MarketDataUpdateService>();

// Register live trading background service
builder.Services.AddHostedService<LiveTradingBackgroundService>();

// Configure Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

// Add Authorization services
builder.Services.AddAuthorization();

var app = builder.Build();

// *** NO ENTITY FRAMEWORK MIGRATIONS NEEDED ***
// With simplified DAL, we use direct SQL scripts instead of EF migrations

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Use request localization to enforce en-US culture for decimal parsing
app.UseRequestLocalization();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();


app.Run();
