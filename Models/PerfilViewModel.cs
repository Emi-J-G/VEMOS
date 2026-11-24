namespace VEMOS.Models;

public class PerfilViewModel
{
    public Usuario Usuario { get; set; } = new();
    public string NombreCompleto { get; set; } = "Tu perfil";
    public string Inicial { get; set; } = "U";
    public IReadOnlyList<Restriccion> Restricciones { get; set; } = Array.Empty<Restriccion>();
    public IReadOnlyList<Usuario> Amigos { get; set; } = Array.Empty<Usuario>();
    public IReadOnlyList<Restaurante> Favoritos { get; set; } = Array.Empty<Restaurante>();
    public string Mensaje { get; set; } = string.Empty;
}
