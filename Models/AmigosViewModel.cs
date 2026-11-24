namespace VEMOS.Models;

public class AmigosViewModel
{
    public IReadOnlyList<UsuarioListaViewModel> Amigos { get; set; } = Array.Empty<UsuarioListaViewModel>();
    public IReadOnlyList<UsuarioListaViewModel> ResultadosBusqueda { get; set; } = Array.Empty<UsuarioListaViewModel>();
    public string Busqueda { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
}
