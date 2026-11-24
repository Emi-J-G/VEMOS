namespace VEMOS.Models;

public class HomeViewModel
{
    public IReadOnlyList<Restriccion> FiltrosActivos { get; set; } = Array.Empty<Restriccion>();
    public IReadOnlyList<string> RestriccionesDisponibles { get; set; } = Array.Empty<string>();
    public IReadOnlyList<Plato> PlatosFavoritos { get; set; } = Array.Empty<Plato>();
    public IReadOnlyList<Restaurante> RestaurantesFavoritos { get; set; } = Array.Empty<Restaurante>();
    public bool HayFavoritos { get; set; }
    public string TituloFavoritos { get; set; } = "Sin favoritos";
}
