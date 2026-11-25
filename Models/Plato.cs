namespace VEMOS.Models;

public class Plato
{
    public int id { get; set; }
    public int idRestaurante { get; set; }
    public string nombre { get; set; }
    public string descripcion { get; set; }
    public decimal precio { get; set; }
    public string tipo { get; set; }
    public string foto { get; set; }

    public string FotoUrl
    {
        get
        {
            if (string.IsNullOrWhiteSpace(foto)) return "/Images/plato-1.png";
            return foto.StartsWith("/", StringComparison.Ordinal) || foto.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? foto
                : $"/Images/{foto}";
        }
    }
}