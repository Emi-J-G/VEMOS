namespace VEMOS.Models;

public class ChatViewModel
{
    public IReadOnlyList<ConversacionChatViewModel> Conversaciones { get; set; } = Array.Empty<ConversacionChatViewModel>();
    public IReadOnlyList<MensajeChatViewModel> Mensajes { get; set; } = Array.Empty<MensajeChatViewModel>();
    public int ConversacionActiva { get; set; }
    public string UsuarioActual { get; set; } = string.Empty;
    public bool HayConversacionSeleccionada { get; set; }
    public string NombreOtroUsuario { get; set; } = string.Empty;
    public string InicialOtroUsuario { get; set; } = "U";
    public string UsernameOtroUsuario { get; set; } = string.Empty;
}
