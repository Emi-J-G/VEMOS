namespace VEMOS.Models;

public class MapaViewModel
{
    public IReadOnlyList<MapaFiltroViewModel> Filtros { get; set; } = Array.Empty<MapaFiltroViewModel>();
    public string RestaurantesJson { get; set; } = "[]";
    public string RestriccionesUsuarioJson { get; set; } = "[]";
    public string RestriccionesTodosJson { get; set; } = "[]";
    public int CantidadRestriccionesActivas { get; set; }
}
