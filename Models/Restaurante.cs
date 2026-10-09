namespace VEMOS.Models;

public class Restaurante
{
    public int id { get; set; }
    public string nombre { get; set; }
    public string direccion { get; set; }
    public string descripcion { get; set; }
    public string telefono { get; set; }
    public List<Plato> platos { get; set; }
}