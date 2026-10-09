namespace VEMOS.Models;

public class ReseñaRestaurante
{
    public int id { get; set; }
    public int idRestaurante { get; set; }
    public int idUsuario { get; set; }
    public string texto { get; set; }
    public int calificacion { get; set; }
}