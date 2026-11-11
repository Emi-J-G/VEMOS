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
}