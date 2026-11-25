namespace VEMOS.Models;

public class RestauranteVistaViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string FotoUrl { get; set; } = string.Empty;
    public IReadOnlyList<string> Restricciones { get; set; } = Array.Empty<string>();
}
