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

    private static List<Restaurante> GetRestaurantesDemo()
    {
        return new List<Restaurante>
        {
            new Restaurante
            {
                id = 1,
                nombre = "La Trattoria",
                direccion = "Av. Corrientes 1234, CABA",
                descripcion = "Pasta y cocina italiana con opciones aptas para celíacos y vegetarianos.",
                telefono = "+54 11 4567-8901",
                tipo = "Italiano",
                lat = -34.6039,
                lng = -58.3835,
                restricciones = new List<string> { "Celiaquía", "Vegetariano" }
            },
            new Restaurante
            {
                id = 2,
                nombre = "Bamboo House",
                direccion = "Av. Santa Fe 2000, CABA",
                descripcion = "Comida asiática con platos veganos y opciones de baja carga glucémica.",
                telefono = "+54 11 4321-7788",
                tipo = "Asiático",
                lat = -34.6017,
                lng = -58.3782,
                restricciones = new List<string> { "Vegano", "Vegetariano", "Celiaquía" }
            },
            new Restaurante
            {
                id = 3,
                nombre = "Verde Mío",
                direccion = "Calle Defensa 887, CABA",
                descripcion = "Menú saludable con platos sin lactosa y aptos para diabetes.",
                telefono = "+54 11 5132-1212",
                tipo = "Saludable",
                lat = -34.6064,
                lng = -58.3920,
                restricciones = new List<string> { "Vegano", "Diabetes", "Lactosa" }
            },
            new Restaurante
            {
                id = 4,
                nombre = "El Patio",
                direccion = "Pasaje Rivadavia 45, CABA",
                descripcion = "Ambiente familiar con opciones kosher y halal para grupos diversos.",
                telefono = "+54 11 4888-3333",
                tipo = "Familiar",
                lat = -34.5998,
                lng = -58.3864,
                restricciones = new List<string> { "Vegetariano", "Kosher", "Halal" }
            },
            new Restaurante
            {
                id = 5,
                nombre = "Cocina de la Plaza",
                direccion = "Plaza de Mayo 55, CABA",
                descripcion = "Restaurante casero con menús pensados para alergias e intolerancias.",
                telefono = "+54 11 5623-4411",
                tipo = "Casero",
                lat = -34.6110,
                lng = -58.3738,
                restricciones = new List<string> { "Celiaquía", "Alergias", "Diabetes" }
            }
        };
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
        var datosUsuario = HttpContext.Session.GetString("UsuarioActual");
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

        var usuarioSesion = ObtenerUsuarioSesion();
        var usuarioId = usuarioSesion?.id ?? 0;

        if (usuarioId > 0)
        {
            var platosFavoritos = db.ObtenerPlatosFavoritos(usuarioId);
            var restaurantesFavoritos = db.ObtenerRestaurantesFavoritos(usuarioId);

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
        var restricciones = db.GetRestriccionesByUsuario(usuario);
        var restaurantes = db.GetRestaurantes();

        if (!restaurantes.Any())
        {
            restaurantes = GetRestaurantesDemo();
        }

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

        var db = new DB();
        var restaurantes = db.GetRestaurantes();
        var restaurante = restaurantes
            .FirstOrDefault(r => string.Equals(r.nombre, nombre, StringComparison.OrdinalIgnoreCase));

        if (restaurante == null)
        {
            restaurante = GetRestaurantesDemo()
                .FirstOrDefault(r => string.Equals(r.nombre, nombre, StringComparison.OrdinalIgnoreCase));
        }

        if (restaurante == null)
        {
            return NotFound();
        }

        var datosUsuario = HttpContext.Session.GetString("UsuarioActual");
        var usuario = string.IsNullOrWhiteSpace(datosUsuario)
        ? null
        : JsonSerializer.Deserialize<Usuario>(datosUsuario);
        ViewBag.EsFavorito = usuario != null &&
        db.ObtenerIdsRestaurantesFavoritos(usuario.id).Contains(restaurante.id);

        return View(restaurante);
    }

    [HttpGet("/Buscar")]
    public IActionResult Buscar(string q, string filtro)
    {
        ViewBag.Query = q?.Trim() ?? string.Empty;
        ViewBag.Filtro = filtro ?? string.Empty;
        var restaurantes = new DB().GetRestaurantes();
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
