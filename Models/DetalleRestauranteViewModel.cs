namespace VEMOS.Models;

public class DetalleRestauranteViewModel
{
    public Restaurante Restaurante { get; set; } = new();
    public bool EsFavorito { get; set; }
    public string FotoUrl { get; set; } = string.Empty;
    public string TextoFavorito { get; set; } = "Guardar favorito";
}
