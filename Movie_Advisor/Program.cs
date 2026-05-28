using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using Movie_Advisor.Data;
using Movie_Advisor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OracleDbContext")));

builder.Services.AddDistributedMemoryCache();

builder.Services.AddScoped<IActivityService, ActivityService>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddHttpClient("recaptcha", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.BaseAddress = new Uri("https://www.google.com/recaptcha/api/");
});

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IActivityService, ActivityService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/User/Login";
    options.LogoutPath = "/User/Logout";
    options.AccessDeniedPath = "/User/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = "MovieAdvisor.Auth";
})
.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
    options.CallbackPath = new PathString("/signin-google");
    options.SaveTokens = true;
    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");

    options.ClaimActions.MapJsonKey("urn:google:picture", "picture", "url");
    options.ClaimActions.MapJsonKey("urn:google:locale", "locale", "string");

    options.Events.OnRedirectToAuthorizationEndpoint = context =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Redirecting to Google Auth: {Uri}", context.RedirectUri);
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };

    options.Events.OnRemoteFailure = context =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError("Google Authentication Failed: {Error}", context.Failure?.Message);
        var errorMessage = context.Failure?.Message ?? "Authentication failed";
        context.Response.Redirect($"/User/Login?error={Uri.EscapeDataString(errorMessage)}");
        context.HandleResponse();
        return Task.CompletedTask;
    };

    options.Events.OnTicketReceived = context =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        var email = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        logger.LogInformation("Google Auth Success — User: {Email}", email ?? "Unknown");
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole("User", "Admin"));
});

builder.Services.AddMvc(options => { });

builder.Services.AddHttpClient<TmdbService>();

builder.Services.AddScoped<YouTubeApiService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var apiKey = config["APIKeys:YouTube"] ?? "";

    var logger = sp.GetRequiredService<ILogger<Program>>();

    if (string.IsNullOrEmpty(apiKey))
    {
        logger.LogWarning("⚠️ YouTube API Key NOT configured!");
    }
    else
    {
        logger.LogInformation("✅ YouTube API Key loaded successfully (length: {Length})", apiKey.Length);
    }

    return new YouTubeApiService(apiKey);
});

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("=== APPLICATION STARTING ===");
logger.LogInformation("Environment: {Env}", app.Environment.EnvironmentName);

var youtubeKey = builder.Configuration["APIKeys:YouTube"];
var tmdbKey = builder.Configuration["APIKeys:TMDB"];
var emailFromAddress = builder.Configuration["Email:FromEmail"];

logger.LogInformation("YouTube API Key Configured: {Status}",
    string.IsNullOrEmpty(youtubeKey) ? "NO ❌" : "YES ✅");
logger.LogInformation("TMDB API Key Configured: {Status}",
    string.IsNullOrEmpty(tmdbKey) ? "NO ❌" : "YES ✅");
logger.LogInformation("Email Service Configured: {Status}",
    string.IsNullOrEmpty(emailFromAddress) ? "NO ❌" : "YES ✅ ({EmailFromAddress})", emailFromAddress ?? "");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "topimdb",
    pattern: "topimdb/{type?}/{decade?}/{genre?}/{award?}",
    defaults: new { controller = "Advisor", action = "TopIMDB" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Lifetime.ApplicationStarted.Register(() =>
{
    logger.LogInformation("=== APPLICATION URLS ===");
    foreach (var address in app.Urls)
    {
        logger.LogInformation("Listening on: {Address}", address);
        logger.LogInformation("Google Callback: {Address}/signin-google", address);
    }
});

app.Run();