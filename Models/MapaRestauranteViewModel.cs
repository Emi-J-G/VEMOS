namespace VEMOS.Models;

public class MapaRestauranteViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public IReadOnlyList<string> Restricciones { get; set; } = Array.Empty<string>();
}
