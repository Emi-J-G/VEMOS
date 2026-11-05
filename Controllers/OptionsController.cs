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

        return View(usuario);
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

        ViewBag.Amigos = new DB().ObtenerAmigos(usuario.id);
        return View();
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

    public IActionResult Reportar()
    {
        return View();
    }
}
