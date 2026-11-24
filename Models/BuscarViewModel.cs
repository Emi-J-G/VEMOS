namespace VEMOS.Models;

public class BuscarViewModel
{
    public IReadOnlyList<Restaurante> Restaurantes { get; set; } = Array.Empty<Restaurante>();
    public string Query { get; set; } = string.Empty;
    public string Filtro { get; set; } = string.Empty;
    public string TituloResultados { get; set; } = "Lugares para descubrir";
}
