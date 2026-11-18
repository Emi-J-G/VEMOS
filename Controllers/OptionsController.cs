using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using VEMOS.Models;

namespace VEMOS.Controllers;

public class OptionsController : Controller
{
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

    [HttpGet("/Options/Perfil")]
    public IActionResult Perfil(string mensaje = null)
    {
        Usuario usuario = ObtenerUsuarioSesion();
        if (usuario == null)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewBag.Mensaje = mensaje;
        DB db = new DB();
        ViewBag.Amigos = db.ObtenerAmigos(usuario.id);
        ViewBag.RestaurantesFavoritos = db.ObtenerRestaurantesFavoritos(usuario.id);
        return View(usuario);
    }

    [HttpGet("/Favoritos")]
    [HttpGet("/Options/Favoritos")]
    public IActionResult Favoritos()
    {
        Usuario usuario = ObtenerUsuarioSesion();
        if (usuario == null)
        {
            return RedirectToAction("Index", "Home");
        }

        DB db = new DB();
        ViewBag.Favoritos = db.ObtenerRestaurantesFavoritos(usuario.id);
        return View();
    }

    [HttpPost("/Options/Perfil/Editar")]
    public IActionResult EditarPerfil(string username, string nombre, string apellido, string email)
    {
        Usuario usuario = ObtenerUsuarioSesion();
        if (usuario == null)
            return RedirectToAction("Index", "Home");

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(nombre) ||
            string.IsNullOrWhiteSpace(apellido) || string.IsNullOrWhiteSpace(email))
        {
            ViewBag.Mensaje = "Completá todos los datos del perfil.";
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
    public IActionResult Amigos(string mensaje = null)
    {
        DB db = new DB();
        Usuario usuario = ObtenerUsuarioSesion();
        if (usuario == null)
            return RedirectToAction("Index", "Home");

        ViewBag.Mensaje = mensaje;
        ViewBag.Amigos = db.ObtenerAmigos(usuario.id);
        ViewBag.Busqueda = string.Empty;
        ViewBag.ResultadosBusqueda = Array.Empty<Usuario>();
        return View();
    }

    [HttpGet("/Options/Usuarios/Buscar")]
    public IActionResult BuscarUsuarios(string q = "")
    {
        DB db = new DB();
        Usuario usuario = ObtenerUsuarioSesion();
        if (usuario == null)
            return RedirectToAction("Index", "Home");

        ViewBag.Amigos = db.ObtenerAmigos(usuario.id);
        ViewBag.Busqueda = q?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(q))
        {
            ViewBag.ResultadosBusqueda = Enumerable.Empty<Usuario>();
            return View("Amigos");
        }

        List<Restriccion> restriccionesUsuario = usuario.restricciones;
        if (restriccionesUsuario == null || !restriccionesUsuario.Any())
            restriccionesUsuario = db.GetRestriccionesByUsuario(usuario.username);

        List<Usuario> resultados = db.BuscarUsuariosPorTextoORestriccion(q, usuario.id);

        if (restriccionesUsuario != null && restriccionesUsuario.Any())
        {
            HashSet<int> idsCompatibles = db.GetUsuariosByRestriccion(restriccionesUsuario)
                .Where(resultado => resultado.id != usuario.id)
                .Select(resultado => resultado.id)
                .ToHashSet();

            resultados = resultados
                .Where(resultado => idsCompatibles.Contains(resultado.id))
                .ToList();
        }

        ViewBag.ResultadosBusqueda = resultados;
        return View("Amigos");
    }

    [HttpGet("/Options/Usuarios/Perfil")]
    public IActionResult PerfilPublico(string username)
    {
        DB db = new DB();
        Usuario usuarioActual = ObtenerUsuarioSesion();
        if (usuarioActual == null)
            return RedirectToAction("Index", "Home");

        Usuario perfil = db.ObtenerPerfilPublico(username?.Trim() ?? string.Empty);
        if (perfil == null || perfil.id == usuarioActual.id)
            return RedirectToAction(nameof(Amigos));

        ViewBag.RestaurantesFavoritos = db.ObtenerRestaurantesFavoritos(perfil.id);
        ViewBag.EsAmigo = db.SonAmigos(usuarioActual.id, perfil.id);
        return View(perfil);
    }

    [HttpPost("/FamiliaAmigos/Agregar")]
    public IActionResult AgregarAmigo(string identificador)
    {
        DB db = new DB();
        Usuario usuario = ObtenerUsuarioSesion();
        if (usuario == null)
            return RedirectToAction("Index", "Home");

        if (string.IsNullOrWhiteSpace(identificador))
        {
            ViewBag.Mensaje = "Ingresá un usuario o correo electrónico.";
        }

        if (!new DB().AgregarAmigo(usuario.id, identificador))
        {
            ViewBag.Mensaje = "No encontramos a esa persona o ya forma parte de tus amigos.";
        }
        else
        {
            ViewBag.Mensaje = "¡Amigo agregado!";
        }

        return RedirectToAction(nameof(Amigos));
    }

    [HttpGet("/Options/Amigos/Lista")]
    public IActionResult ListaAmigos()
    {
        DB db = new DB();
        Usuario usuario = ObtenerUsuarioSesion();
        if (usuario == null)
            return Unauthorized();

        return Json(db.ObtenerAmigos(usuario.id));
    }

    [HttpPost("/Options/Amigos/AgregarFetch")]
    public IActionResult AgregarAmigoFetch([FromForm] string identificador)
    {
        DB db = new DB();
        Usuario usuario = ObtenerUsuarioSesion();
        if (usuario == null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(identificador))
            return BadRequest(new { message = "Ingresá un usuario o correo electrónico." });

        if (!new DB().AgregarAmigo(usuario.id, identificador))
            return Conflict(new { message = "No encontramos a esa persona o ya forma parte de tus amigos." });

        return Ok(new { message = "¡Amigo agregado!", amigos = db.ObtenerAmigos(usuario.id) });
    }

    [HttpPost("/Options/Amigos/Eliminar")]
    public IActionResult EliminarAmigo([FromForm] int idAmigo)
    {
        DB db = new DB();
        Usuario usuario = ObtenerUsuarioSesion();
        if (usuario == null)
            return RedirectToAction("Index", "Home");

        if (idAmigo <= 0)
            return RedirectToAction(nameof(Amigos), new { mensaje = "No se pudo eliminar el amigo." });

        db.EliminarAmigo(usuario.id, idAmigo);
        return RedirectToAction(nameof(Amigos), new { mensaje = "Amigo eliminado de tu lista." });
    }

    [HttpPost("/Options/Favoritos/Toggle")]
    public IActionResult AlternarFavorito([FromForm] int idRestaurante)
    {
        DB db = new DB();
        Usuario usuario = ObtenerUsuarioSesion();
        if (usuario == null)
            return Unauthorized();

        bool favorito = db.AlternarRestauranteFavorito(usuario.id, idRestaurante);
        return Ok(new { favorito });
    }

    [HttpGet("/Options/Favoritos/Lista")]
    public IActionResult ListaFavoritos()
    {
        DB db = new DB();
        Usuario usuario = ObtenerUsuarioSesion();
        if (usuario == null)
            return Unauthorized();

        return Json(db.ObtenerRestaurantesFavoritos(usuario.id));
    }

    [HttpPost("/Options/Filtros/Guardar")]
    public IActionResult GuardarFiltros([FromBody] string[] restricciones)
    {
        DB db = new DB();
        Usuario usuario = ObtenerUsuarioSesion();
        if (usuario == null)
            return Unauthorized();

        db.ActualizarRestriccionesUsuario(usuario.id, restricciones ?? Array.Empty<string>());
        usuario.restricciones = db.GetRestriccionesByUsuario(usuario.username);
        HttpContext.Session.SetString("UsuarioActual", JsonSerializer.Serialize(usuario));
        return Ok(new { restricciones = usuario.restricciones.Select(restriccion => restriccion.nombre) });
    }

    public IActionResult Reportar()
    {
        return View();
    }

    [HttpGet("/Chat")]
    public IActionResult Chat(int? idConversacion = null)
    {
        Usuario usuario = ObtenerUsuarioSesion();
        if (usuario == null)
            return RedirectToAction("Index", "Home");

        DB db = new DB();
        List<ConversacionChat> conversaciones = db.ObtenerConversacionesChat(usuario.id);

        int conversacionActiva = idConversacion ?? conversaciones.FirstOrDefault()?.id ?? 0;
        Usuario? otroUsuario = conversacionActiva > 0
            ? db.ObtenerUsuarioConversacion(conversacionActiva, usuario.id)
            : null;

        ViewBag.Conversaciones = conversaciones;
        ViewBag.ConversacionActiva = conversacionActiva;
        ViewBag.OtroUsuario = otroUsuario;
        ViewBag.Mensajes = conversacionActiva > 0 && otroUsuario != null
            ? db.ObtenerMensajesChat(conversacionActiva)
            : new List<MensajeChat>();

        return View("Chat");
    }

    [HttpPost("/Chat/Iniciar")]
    public IActionResult IniciarChat([FromForm] string username)
    {
        Usuario usuario = ObtenerUsuarioSesion();
        if (usuario == null)
            return RedirectToAction("Index", "Home");

        DB db = new DB();
        Usuario? destino = db.ObtenerPerfilPublico(username?.Trim() ?? string.Empty);
        if (destino == null || destino.id == usuario.id)
            return RedirectToAction(nameof(Amigos), new { mensaje = "No se pudo iniciar ese chat." });

        int conversacionId = db.ObtenerOCrearConversacion(usuario.id, destino.id);
        return Redirect($"/Chat?idConversacion={conversacionId}");
    }

    [HttpPost("/Chat/Enviar")]
    public IActionResult EnviarMensaje([FromForm] int idConversacion, [FromForm] string texto)
    {
        Usuario usuario = ObtenerUsuarioSesion();
        if (usuario == null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(texto))
            return BadRequest(new { message = "Escribí un mensaje." });

        DB db = new DB();
        if (!db.UsuarioPerteneceAConversacion(usuario.id, idConversacion))
            return Forbid();

        db.AgregarMensajeChat(idConversacion, usuario.id, texto.Trim().Length > 1000 ? texto.Trim()[..1000] : texto.Trim());
        return Ok(new { message = "Mensaje enviado." });
    }

    [HttpGet("/Chat/Mensajes")]
    public IActionResult ObtenerMensajes(int idConversacion)
    {
        Usuario usuario = ObtenerUsuarioSesion();
        if (usuario == null)
            return Unauthorized();

        DB db = new DB();
        if (!db.UsuarioPerteneceAConversacion(usuario.id, idConversacion))
            return Forbid();

        return Json(db.ObtenerMensajesChat(idConversacion));
    }
}
