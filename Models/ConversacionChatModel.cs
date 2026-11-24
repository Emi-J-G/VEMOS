namespace VEMOS.Models;

public class ConversacionChat
{
    public int id { get; set; }
    public int idUsuarioA { get; set; }
    public int idUsuarioB { get; set; }
    public Usuario otroUsuario { get; set; }
    public MensajeChat ultimoMensaje { get; set; }
}
