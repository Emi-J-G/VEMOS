using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using VEMOS.Models;

namespace VEMOS.Controllers;

public class LoginRegisController : Controller
{
    private readonly ILogger<LoginRegisController> _logger;

    public LoginRegisController(ILogger<LoginRegisController> logger)
    {
        _logger = logger;
    }

    private void GuardarUsuarioEnSesion(Usuario usuario)
    {
        HttpContext.Session.SetString("Usuario", usuario.username);
        HttpContext.Session.SetString("NombreCompleto", usuario.nombre ?? string.Empty);
        HttpContext.Session.SetString("UsuarioActual", JsonSerializer.Serialize(usuario));
    }

    [HttpGet("/LoginRegis/GoogleLogin")]
    public IActionResult GoogleLogin(string returnUrl = "/")
    {
        var redirectUrl = Url.Action(nameof(GoogleResponse), "LoginRegis", null, Request.Scheme) ?? "/signin-google";
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("/LoginRegis/GoogleResponse")]
    public async Task<IActionResult> GoogleResponse()
    {
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!result.Succeeded || result.Principal == null)
        {
            return RedirectToAction("Index", "Home", new { mensaje = "No se pudo iniciar sesión con Google." });
        }

        var claims = result.Principal.Claims;
        var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
            ?? claims.FirstOrDefault(c => c.Type == "email")?.Value;
        var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
            ?? claims.FirstOrDefault(c => c.Type == "name")?.Value
            ?? email ?? "Usuario Google";
        var apellido = claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value
            ?? claims.FirstOrDefault(c => c.Type == "family_name")?.Value
            ?? string.Empty;

        DB db = new DB();
        Usuario usuarioGoogle = db.ObtenerUsuarioPorEmail(email);

        if (usuarioGoogle == null)
        {
            usuarioGoogle = new Usuario
            {
                username = email ?? name,
                nombre = name,
                apellido = apellido,
                email = email,
                tipo = "Google",
                contrasena = string.Empty,
                restricciones = new List<Restriccion>()
            };
        }

        HttpContext.Session.SetString("Usuario", usuarioGoogle.username);
        HttpContext.Session.SetString("NombreCompleto", usuarioGoogle.nombre ?? string.Empty);
        HttpContext.Session.SetString("AuthProvider", "Google");
        HttpContext.Session.SetString("UsuarioActual", JsonSerializer.Serialize(usuarioGoogle));

        return RedirectToAction("Home", "Home");
    }

    //desde index manda a view de registro
    [HttpGet("/LoginRegis/Registro")]
    public IActionResult Registro(string mensaje)
    {
        return RedirectToAction("Index", "Home", new { mensaje, abrirRegistro = true });
    }

    //registra al user en la base de datos y lo loguea
    [HttpPost("/LoginRegis/Registrar")]
    public IActionResult Registrar(string nombreUsuario, string nombre, string apellido, string contrasena, string email, string[] restricciones, string tipo)
    {
        var restriccionesUsuario = (restricciones ?? Array.Empty<string>())
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => new Restriccion { nombre = r })
            .ToList();

        Usuario usuario = new Usuario()
        {
            username = nombreUsuario,
            nombre = nombre,
            apellido = apellido,
            email = email,
            contrasena = contrasena,
            tipo = tipo,
            restricciones = restriccionesUsuario
        };

        DB db = new DB();
        if (db.nomUsuarioExiste(nombreUsuario) != null) // en esta instancia, se valida que el nombre de usuario no esté siendo usado
        {
            ViewBag.Mensaje = "El nombre de usuario ya está en uso.";
            return RedirectToAction("Registro", "LoginRegis", new { mensaje = ViewBag.Mensaje });
        }
        else if (db.emUsuarioExiste(email) != null) // en esta otra instancia, se valida que el correo electrónico no esté en uso
        {
            ViewBag.Mensaje = "El correo electrónico ya está en uso.";
            return RedirectToAction("Registro", "LoginRegis", new { mensaje = ViewBag.Mensaje });
        }
        else // al pasar ambas instancias, se registra el usuario en la base de datos y se loguea automáticamente
        {
            db.registrarUsuario(usuario);
        }

        GuardarUsuarioEnSesion(usuario);
        return RedirectToAction("Home", "Home");
    }

    [HttpGet("/LoginRegis/Login")]
    public IActionResult Login(string mensaje)
    {
        ViewBag.Mensaje = mensaje;
        return View();
    }

    //logea un user
    [HttpPost("/LoginRegis/Loguear")]
    public IActionResult Loguear(string nombreUsuario, string contrasena) {
        if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrWhiteSpace(contrasena))
        {
            return RedirectToAction("Index", "Home", new { mensaje = "Debe completar usuario y contraseña." });
        }

        DB db = new DB();
        Usuario usuario = db.nomUsuarioExiste(nombreUsuario);

        if (usuario != null)
        {
            usuario = db.validarUsuario(usuario.email, contrasena);
        }

        if (usuario != null) {
            GuardarUsuarioEnSesion(usuario);
            return RedirectToAction("Home", "Home");
        }
        else {
            //usuario no encontrado o contraseña incorrecta
            return RedirectToAction("Index", "Home", new { mensaje = "Usuario o contraseña incorrectos." });
        }
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
