namespace VEMOS.Models;

public class Usuario
{
    public int id { get; set; }
    public string nombre { get; set; }
    public string apellido { get; set; }
    public string email { get; set; }
    public string contrasena { get; set; }
    public List<Restriccion> restricciones { get; set; }
}