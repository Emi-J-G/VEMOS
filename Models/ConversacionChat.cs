namespace VEMOS.Models;

public class ConversacionChat
{
    public int id { get; set; }
    public int idUsuarioA { get; set; }
    public int idUsuarioB { get; set; }
    public Usuario otroUsuario { get; set; }
    public MensajeChat ultimoMensaje { get; set; }
}

public class MensajeChat
{
    public int id { get; set; }
    public int idConversacion { get; set; }
    public int idRemitente { get; set; }
    public string texto { get; set; } = string.Empty;
    public DateTime fecha { get; set; }
    public string remitenteNombre { get; set; } = string.Empty;
}

public class MensajeChatViewModel
{
    public string Texto { get; set; } = string.Empty;
    public string RemitenteNombre { get; set; } = string.Empty;
    public string Hora { get; set; } = string.Empty;
    public bool Propio { get; set; }
}

public class ConversacionChatViewModel
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string Inicial { get; set; } = "U";
    public string UltimoMensaje { get; set; } = "Iniciar conversación";
    public bool Activa { get; set; }
}

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
