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
        int usuarioId;
        if (usuarioSesion != null)
        {
            usuarioId = usuarioSesion.id;
        }
        else
        {
            usuarioId = 0;
        }

        if (usuarioId > 0)
        {
            List<Plato> platosFavoritos = db.ObtenerPlatosFavoritos(usuarioId);
            List<Restaurante> restaurantesFavoritos = db.ObtenerRestaurantesFavoritos(usuarioId);

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
            return RedirectToAction("Index");

        DB db = new DB();
        string usuario = HttpContext.Session.GetString("Usuario");
        List<Restriccion> restricciones = db.GetRestriccionesByUsuario(usuario);
        List<Restaurante> restaurantes = db.GetRestaurantes();

        ViewBag.RestriccionesUsuario = restricciones;
        ViewBag.RestriccionesDisponibles = new[]
            db.GetRestricciones();

        return View(restaurantes);
    }

    public IActionResult DetalleRestaurante(string nombre)
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("Usuario")))
            return RedirectToAction("Index");

        DB db = new DB();
        List<Restaurante> restaurantes = db.GetRestaurantes();
        Restaurante restaurante = restaurantes
            .FirstOrDefault(r => string.Equals(r.nombre, nombre, StringComparison.OrdinalIgnoreCase));

        if (restaurante == null)
            return NotFound();

        Usuario usuario = ObtenerUsuarioSesion();
        ViewBag.EsFavorito = usuario != null && db.ObtenerIdsRestaurantesFavoritos(usuario.id).Contains(restaurante.id);

        return View(restaurante);
    }

    [HttpGet("/Buscar")]
    public IActionResult Buscar(string q, string filtro)
    {
        string query = q;
        if (query == null)
                query = string.Empty;
    
        else
            query = query.Trim();

        string filtroActual = filtro;
        if (filtroActual == null)
            filtroActual = string.Empty;

        ViewBag.Query = query;
        ViewBag.Filtro = filtroActual;

        List<Restaurante> restaurantes = new DB().GetRestaurantes();
        if (!string.IsNullOrWhiteSpace(query))
        {
            restaurantes = restaurantes
                .Where(r =>
                {
                    string nombre = r.nombre;
                    if (nombre == null)
                        nombre = string.Empty;

                    string direccion = r.direccion;
                    if (direccion == null)
                        direccion = string.Empty;

                    string descripcion = r.descripcion;
                    if (descripcion == null)
                        descripcion = string.Empty;

                    bool coincideNombre = nombre.Contains(query, StringComparison.OrdinalIgnoreCase);
                    bool coincideDireccion = direccion.Contains(query, StringComparison.OrdinalIgnoreCase);
                    bool coincideDescripcion = descripcion.Contains(query, StringComparison.OrdinalIgnoreCase);

                    if (coincideNombre)
                        return true;
                        
                    if (coincideDireccion)
                        return true;
                        
                    if (coincideDescripcion)
                        return true;
                        
                    return false;
                })
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
