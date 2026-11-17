using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSession();
builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/LoginRegis/Login";
    options.AccessDeniedPath = "/LoginRegis/Login";
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
    options.CallbackPath = "/signin-google";
    options.SaveTokens = true;
    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Events.OnRemoteFailure = context =>
    {
        string message = Uri.EscapeDataString("No se pudo completar el inicio de sesión con Google. Volvé a intentarlo.");
        context.Response.Redirect($"/?mensaje={message}");
        context.HandleResponse();
        return Task.CompletedTask;
    };
    options.Events.OnCreatingTicket = context =>
    {
        ClaimsPrincipal principal = context.Principal;
        if (principal == null)
        {
            return Task.CompletedTask;
        }

        string? email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("email");
        string? name = principal.FindFirstValue(ClaimTypes.Name) ?? principal.FindFirstValue("name");
        string username = email ?? name ?? "google-user";

        context.HttpContext.Session.SetString("Usuario", username);
        context.HttpContext.Session.SetString("NombreCompleto", name ?? email ?? "Usuario Google");
        context.HttpContext.Session.SetString("AuthProvider", "Google");

        return Task.CompletedTask;
    };
});

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "mapa",
    pattern: "Mapa",
    defaults: new { controller = "Home", action = "Mapa" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
