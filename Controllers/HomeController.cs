using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using VEMOS.Models;

namespace VEMOS.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index(string mensaje, bool abrirRegistro = false)
    {
        if (!string.IsNullOrEmpty(HttpContext.Session.GetString("Usuario")))
        {
            return RedirectToAction("Home");
        }

        ViewBag.Usuario = HttpContext.Session.GetString("Usuario");
        ViewBag.NombreCompleto = HttpContext.Session.GetString("NombreCompleto");
        ViewBag.Mensaje = mensaje;
        ViewBag.AbrirRegistro = abrirRegistro;
        return View();
    }

    private Usuario? ObtenerUsuarioSesion()
    {
        string datosUsuario = HttpContext.Session.GetString("UsuarioActual");
        if (string.IsNullOrWhiteSpace(datosUsuario))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Usuario>(datosUsuario);
        }
        catch
        {
            return null;
        }
    }

    public IActionResult Home()
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("Usuario")))
        {
            return RedirectToAction("Index");
        }

        DB db = new DB();
        List<Restriccion> filtrosActivos = new List<Restriccion>();
        string usuario = HttpContext.Session.GetString("Usuario");
        filtrosActivos = db.GetRestriccionesByUsuario(usuario);
        ViewBag.filtrosActivos = filtrosActivos;

        Usuario usuarioSesion = ObtenerUsuarioSesion();
        int usuarioId = usuarioSesion?.id ?? 0;

        if (usuarioId > 0)
        {
            PlatoFavorito platosFavoritos = db.ObtenerPlatosFavoritos(usuarioId);
            RestauranteFavorito restaurantesFavoritos = db.ObtenerRestaurantesFavoritos(usuarioId);

            ViewBag.PlatosFavoritos = platosFavoritos;
            ViewBag.RestaurantesFavoritos = restaurantesFavoritos;
            ViewBag.HayFavoritos = platosFavoritos.Any() || restaurantesFavoritos.Any();
        }
        else
        {
            ViewBag.PlatosFavoritos = new List<Plato>();
            ViewBag.RestaurantesFavoritos = new List<Restaurante>();
            ViewBag.HayFavoritos = false;
        }

        return View();
    }

    public IActionResult Mapa()
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("Usuario")))
        {
            return RedirectToAction("Index");
        }

        DB db = new DB();
        string usuario = HttpContext.Session.GetString("Usuario");
        Restriccion restricciones = db.GetRestriccionesByUsuario(usuario);
        Restaurante restaurantes = db.GetRestaurantes();

        ViewBag.RestriccionesUsuario = restricciones;
        ViewBag.RestriccionesDisponibles = new[]
        {
            "Celiaquía",
            "Diabetes",
            "Kosher",
            "Halal",
            "Lactosa",
            "Vegano",
            "Vegetariano",
            "Alergias"
        };

        return View(restaurantes);
    }

    public IActionResult DetalleRestaurante(string nombre)
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("Usuario")))
        {
            return RedirectToAction("Index");
        }

        DB db = new DB();
        Restaurante restaurantes = db.GetRestaurantes();
        Restaurante restaurante = restaurantes
            .FirstOrDefault(r => string.Equals(r.nombre, nombre, StringComparison.OrdinalIgnoreCase));

        if (restaurante == null)
        {
            return NotFound();
        }

        Usuario usuario = ObtenerUsuarioSesion();
        ViewBag.EsFavorito = usuario != null &&
            db.ObtenerIdsRestaurantesFavoritos(usuario.id).Contains(restaurante.id);

        return View(restaurante);
    }

    [HttpGet("/Buscar")]
    public IActionResult Buscar(string q, string filtro)
    {
        ViewBag.Query = q?.Trim() ?? string.Empty;
        ViewBag.Filtro = filtro ?? string.Empty;

        Restaurante restaurantes = new DB().GetRestaurantes();
        if (!string.IsNullOrWhiteSpace(q))
        {
            restaurantes = restaurantes
                .Where(r => (r.nombre ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase)
                    || (r.direccion ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase)
                    || (r.descripcion ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return View(restaurantes);
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
