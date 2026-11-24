namespace VEMOS.Models;

public class PerfilPublicoViewModel
{
    public Usuario Usuario { get; set; } = new();
    public string NombreCompleto { get; set; } = string.Empty;
    public string Inicial { get; set; } = "U";
    public string FotoUrl { get; set; } = string.Empty;
    public bool EsAmigo { get; set; }
    public IReadOnlyList<Restaurante> Favoritos { get; set; } = Array.Empty<Restaurante>();
}
