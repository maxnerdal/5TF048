using WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Temporarily disable antiforgery validation for development
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryTokenAttribute());
});

// Add memory cache for session storage
builder.Services.AddMemoryCache();

// Register our custom session service for draft functionality
builder.Services.AddSingleton<ISimpleSessionService, SimpleSessionService>();

// Register HttpClient and BTC Price Service
builder.Services.AddHttpClient<IBtcPriceService, BtcPriceService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Portfolio}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
