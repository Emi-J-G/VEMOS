namespace VEMOS.Models;

public class Usuario
{
    public int id { get; set; }
    public string username { get; set; }
    public string nombre { get; set; }
    public string apellido { get; set; }
    public string email { get; set; }
    public string tipo { get; set; }
    public string contrasena { get; set; }
    public string foto { get; set; }
    public List<Restriccion> restricciones { get; set; }

    public string NombreCompleto
    {
        get
        {
            string nombreCompleto = string.Join(" ", new[] { nombre, apellido }.Where(valor => !string.IsNullOrWhiteSpace(valor)));
            return string.IsNullOrWhiteSpace(nombreCompleto) ? username ?? "usuario" : nombreCompleto;
        }
    }

    public string Inicial
    {
        get
        {
            string valor = !string.IsNullOrWhiteSpace(NombreCompleto) ? NombreCompleto : username;
            return !string.IsNullOrWhiteSpace(valor) ? valor.Trim()[0].ToString().ToUpperInvariant() : "U";
        }
    }

    public string FotoUrl
    {
        get
        {
            if (string.IsNullOrWhiteSpace(foto)) return "/Images/Arturito.jpg";
            return foto.StartsWith("/", StringComparison.Ordinal) || foto.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? foto
                : $"/Images/{foto}";
        }
    }
}