namespace VEMOS.Models;

public class ReseñaPlato
{
    public int id { get; set; }
    public int idPlato { get; set; }
    public int idUsuario { get; set; }
    public string texto { get; set; }
    public int calificacion { get; set; }
}