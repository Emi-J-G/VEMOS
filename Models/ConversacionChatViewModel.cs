namespace VEMOS.Models;

public class ConversacionChatViewModel
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string Inicial { get; set; } = "U";
    public string UltimoMensaje { get; set; } = "Iniciar conversación";
    public bool Activa { get; set; }
}
