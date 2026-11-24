namespace VEMOS.Models;

public class MensajeChat
{
    public int id { get; set; }
    public int idConversacion { get; set; }
    public int idRemitente { get; set; }
    public string texto { get; set; } = string.Empty;
    public DateTime fecha { get; set; }
    public string remitenteNombre { get; set; } = string.Empty;
}
