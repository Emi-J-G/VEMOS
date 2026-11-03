namespace VEMOS.Models;

public class Restaurante
{
    public int id { get; set; }
    public string nombre { get; set; }
    public string direccion { get; set; }
    public string descripcion { get; set; }
    public string telefono { get; set; }
    public string tipo { get; set; }
    public double lat { get; set; }
    public double lng { get; set; }
    public List<string> restricciones { get; set; } = new();
    public List<Plato> platos { get; set; }
    public int idUsuario { get; set; }
}