using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace VEMOS.Models;

public class LayoutDataFilter : IAsyncResultFilter
{
    public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Controller is Controller controller)
        {
            string usuario = controller.HttpContext.Session.GetString("Usuario") ?? string.Empty;
            string nombreCompleto = controller.HttpContext.Session.GetString("NombreCompleto") ?? string.Empty;

            string inicial = !string.IsNullOrWhiteSpace(nombreCompleto)
                ? nombreCompleto.Trim()[0].ToString().ToUpperInvariant()
                : !string.IsNullOrWhiteSpace(usuario)
                    ? usuario.Trim()[0].ToString().ToUpperInvariant()
                    : "U";

            controller.ViewBag.UsuarioAutenticado = !string.IsNullOrWhiteSpace(usuario);
            controller.ViewBag.InicialUsuario = inicial;
        }

        return next();
    }
}
