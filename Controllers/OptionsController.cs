using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using VEMOS.Models;

namespace VEMOS.Controllers;

public class OptionsController : Controller
{
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

    [HttpGet("/Options/Perfil")]
    public IActionResult Perfil()
    {
        var usuario = ObtenerUsuarioSesion();
        if (usuario == null)
        {
            return RedirectToAction("Index", "Home");
        }

        var db = new DB();
        ViewBag.Amigos = db.ObtenerAmigos(usuario.id);
        ViewBag.RestaurantesFavoritos = db.ObtenerRestaurantesFavoritos(usuario.id);
        return View(usuario);
    }

    [HttpGet("/Favoritos")]
    [HttpGet("/Options/Favoritos")]
    public IActionResult Favoritos()
    {
        var usuario = ObtenerUsuarioSesion();
        if (usuario == null)
        {
            return RedirectToAction("Index", "Home");
        }

        var favoritos = new DB().ObtenerRestaurantesFavoritos(usuario.id);
        return View(favoritos);
    }

    [HttpPost("/Options/Perfil/Editar")]
    public IActionResult EditarPerfil(string username, string nombre, string apellido, string email)
    {
        var usuario = ObtenerUsuarioSesion();
        if (usuario == null)
            return RedirectToAction("Index", "Home");

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(nombre) ||
            string.IsNullOrWhiteSpace(apellido) || string.IsNullOrWhiteSpace(email))
        {
            TempData["PerfilMensaje"] = "Completá todos los datos del perfil.";
            return RedirectToAction(nameof(Perfil));
        }

        usuario.username = username.Trim();
        usuario.nombre = nombre.Trim();
        usuario.apellido = apellido.Trim();
        usuario.email = email.Trim();

        new DB().ActualizarUsuario(usuario);
        HttpContext.Session.SetString("Usuario", usuario.username);
        HttpContext.Session.SetString("NombreCompleto", usuario.nombre);
        HttpContext.Session.SetString("UsuarioActual", JsonSerializer.Serialize(usuario));
        return RedirectToAction(nameof(Perfil));
    }

    [HttpGet("/Ajustes")]
    [HttpGet("/Options/Ajustes")]
    public IActionResult Ajustes()
    {
        return View();
    }

    [HttpGet("/FamiliaAmigos")]
    [HttpGet("/Options/Amigos")]
    public IActionResult Amigos()
    {
        var usuario = ObtenerUsuarioSesion();
        if (usuario == null)
            return RedirectToAction("Index", "Home");

        var db = new DB();
        ViewBag.Amigos = db.ObtenerAmigos(usuario.id);
        ViewBag.Busqueda = string.Empty;
        ViewBag.ResultadosBusqueda = Array.Empty<Usuario>();
        return View();
    }

    [HttpGet("/Options/Usuarios/Buscar")]
    public IActionResult BuscarUsuarios(string q = "")
    {
        var usuario = ObtenerUsuarioSesion();
        if (usuario == null)
            return RedirectToAction("Index", "Home");

        var db = new DB();
        ViewBag.Amigos = db.ObtenerAmigos(usuario.id);
        ViewBag.Busqueda = q?.Trim() ?? string.Empty;
        ViewBag.ResultadosBusqueda = string.IsNullOrWhiteSpace(q)
            ? Enumerable.Empty<Usuario>()
            : db.BuscarUsuarios(q, usuario.id);
        return View("Amigos");
    }

    [HttpGet("/Options/Usuarios/Perfil")]
    public IActionResult PerfilPublico(string username)
    {
        var usuarioActual = ObtenerUsuarioSesion();
        if (usuarioActual == null)
            return RedirectToAction("Index", "Home");

        var db = new DB();
        var perfil = db.ObtenerPerfilPublico(username?.Trim() ?? string.Empty);
        if (perfil == null || perfil.id == usuarioActual.id)
            return RedirectToAction(nameof(Amigos));

        ViewBag.RestaurantesFavoritos = db.ObtenerRestaurantesFavoritos(perfil.id);
        ViewBag.EsAmigo = db.SonAmigos(usuarioActual.id, perfil.id);
        return View(perfil);
    }

    [HttpPost("/FamiliaAmigos/Agregar")]
    public IActionResult AgregarAmigo(string identificador)
    {
        var usuario = ObtenerUsuarioSesion();
        if (usuario == null)
            return RedirectToAction("Index", "Home");

        if (string.IsNullOrWhiteSpace(identificador))
        {
            TempData["AmigosMensaje"] = "Ingresá un usuario o correo electrónico.";
        }
        else if (!new DB().AgregarAmigo(usuario.id, identificador))
        {
            TempData["AmigosMensaje"] = "No encontramos a esa persona o ya forma parte de tus amigos.";
        }
        else
        {
            TempData["AmigosMensaje"] = "¡Amigo agregado!";
        }

        return RedirectToAction(nameof(Amigos));
    }

    [HttpGet("/Options/Amigos/Lista")]
    public IActionResult ListaAmigos()
    {
        var usuario = ObtenerUsuarioSesion();
        if (usuario == null)
            return Unauthorized();

        return Json(new DB().ObtenerAmigos(usuario.id));
    }

    [HttpPost("/Options/Amigos/AgregarFetch")]
    public IActionResult AgregarAmigoFetch([FromForm] string identificador)
    {
        var usuario = ObtenerUsuarioSesion();
        if (usuario == null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(identificador))
            return BadRequest(new { message = "Ingresá un usuario o correo electrónico." });

        if (!new DB().AgregarAmigo(usuario.id, identificador))
            return Conflict(new { message = "No encontramos a esa persona o ya forma parte de tus amigos." });

        return Ok(new { message = "¡Amigo agregado!", amigos = new DB().ObtenerAmigos(usuario.id) });
    }

    [HttpPost("/Options/Favoritos/Toggle")]
    public IActionResult AlternarFavorito([FromForm] int idRestaurante)
    {
        var usuario = ObtenerUsuarioSesion();
        if (usuario == null)
            return Unauthorized();

        var favorito = new DB().AlternarRestauranteFavorito(usuario.id, idRestaurante);
        return Ok(new { favorito });
    }

    [HttpGet("/Options/Favoritos/Lista")]
    public IActionResult ListaFavoritos()
    {
        var usuario = ObtenerUsuarioSesion();
        if (usuario == null)
            return Unauthorized();

        return Json(new DB().ObtenerRestaurantesFavoritos(usuario.id));
    }

    [HttpPost("/Options/Filtros/Guardar")]
    public IActionResult GuardarFiltros([FromBody] string[] restricciones)
    {
        var usuario = ObtenerUsuarioSesion();
        if (usuario == null)
            return Unauthorized();

        new DB().ActualizarRestriccionesUsuario(usuario.id, restricciones ?? Array.Empty<string>());
        usuario.restricciones = new DB().GetRestriccionesByUsuario(usuario.username);
        HttpContext.Session.SetString("UsuarioActual", JsonSerializer.Serialize(usuario));
        return Ok(new { restricciones = usuario.restricciones.Select(restriccion => restriccion.nombre) });
    }

    public IActionResult Reportar()
    {
        return View();
    }
}
