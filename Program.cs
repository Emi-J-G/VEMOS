using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

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
    options.Events.OnCreatingTicket = context =>
    {
        var principal = context.Principal;
        if (principal == null)
        {
            return Task.CompletedTask;
        }

        var email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("email");
        var name = principal.FindFirstValue(ClaimTypes.Name) ?? principal.FindFirstValue("name");
        var username = email ?? name ?? "google-user";

        context.HttpContext.Session.SetString("Usuario", username);
        context.HttpContext.Session.SetString("NombreCompleto", name ?? email ?? "Usuario Google");
        context.HttpContext.Session.SetString("AuthProvider", "Google");

        return Task.CompletedTask;
    };
});

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
