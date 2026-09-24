using System.Security.Claims;
using FootStats.Web.Components;
using FootStats.Web.Data;
using FootStats.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

var dbPath = builder.Configuration["Database:Path"] ?? "data/footstats.db";
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(dbPath))!);

var uploadsPath = Path.GetFullPath(builder.Configuration["Uploads:Path"] ?? "data/uploads");
Directory.CreateDirectory(uploadsPath);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddScoped<StatsService>();
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<CalendarService>();
builder.Services.AddScoped<ConfirmService>();
builder.Services.AddSingleton(new UploadsPathProvider(uploadsPath));
builder.Services.AddDataProtection();
builder.Services.AddHttpClient<IEmailSender, EmailService>();
builder.Services.AddScoped<InvitationService>();
builder.Services.AddScoped<ShareInviteService>();
builder.Services.AddHostedService<InvitationExpiryHostedService>();
builder.Services.AddHostedService<MatchReminderHostedService>();

var authBuilder = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.Events = ExternalAuthEvents.Build();
    });
}

var microsoftClientId = builder.Configuration["Authentication:Microsoft:ClientId"];
var microsoftClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"];
if (!string.IsNullOrEmpty(microsoftClientId) && !string.IsNullOrEmpty(microsoftClientSecret))
{
    authBuilder.AddMicrosoftAccount(options =>
    {
        options.ClientId = microsoftClientId;
        options.ClientSecret = microsoftClientSecret;
        options.Events = ExternalAuthEvents.Build();
    });
}

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        // Envoie le texte de l'exception au navigateur, que le panneau d'erreur affiche dans "Détails
        // techniques". Actif en développement seulement, car cela expose les traces d'appels aux utilisateurs
        // connectés ; DetailedErrors=true (variable d'environnement) permet de les obtenir aussi en production.
        options.DetailedErrors = builder.Configuration.GetValue("DetailedErrors", builder.Environment.IsDevelopment());
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db, app.Configuration);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
}); // photos/logos uploadés à l'exécution, stockés hors de wwwroot pour survivre aux redéploiements
app.MapStaticAssets().AllowAnonymous();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapPost("/account/login", async (HttpContext http, AppDbContext db) =>
{
    var form = await http.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();
    var returnUrl = form["returnUrl"].ToString();

    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
    if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) || !user.IsActive)
    {
        return Results.Redirect($"/login?error=1&returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    var isFirstLogin = user.HasLoggedInAt is null;
    if (isFirstLogin)
    {
        user.HasLoggedInAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.Name, user.Username),
        new(ClaimTypes.GivenName, user.FirstName),
        new(ClaimTypes.Surname, user.LastName),
        new(ClaimTypes.Role, user.Role.ToString()),
        new("sv", user.SessionVersion.ToString())
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

    if (string.IsNullOrEmpty(returnUrl) && isFirstLogin && user.Role == UserRole.Admin)
    {
        return Results.Redirect("/bienvenue");
    }

    return Results.Redirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
}).AllowAnonymous();

app.MapGet("/account/external-login/{provider}", (string provider, string? returnUrl) =>
{
    var properties = new AuthenticationProperties
    {
        RedirectUri = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl
    };
    return Results.Challenge(properties, [provider]);
}).AllowAnonymous();

app.MapPost("/account/logout", async (HttpContext http, string? returnUrl) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect(string.IsNullOrEmpty(returnUrl) ? "/login" : $"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
});

// Déclenché depuis MainLayout (navigation GET) quand un compte connecté vient d'être supprimé ou révoqué :
// coupe la session en cours sans attendre que la prochaine requête de données échoue.
app.MapGet("/account/force-logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login?ended=1");
}).AllowAnonymous();

app.Run();
