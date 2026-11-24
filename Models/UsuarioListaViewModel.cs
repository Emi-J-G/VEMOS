namespace VEMOS.Models;

public class UsuarioListaViewModel
{
    public Usuario Usuario { get; set; } = new();
    public string NombreCompleto { get; set; } = string.Empty;
    public string Inicial { get; set; } = "U";
    public string FotoUrl { get; set; } = string.Empty;
}
