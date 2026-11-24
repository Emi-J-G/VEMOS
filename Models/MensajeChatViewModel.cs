namespace VEMOS.Models;

public class MensajeChatViewModel
{
    public string Texto { get; set; } = string.Empty;
    public string RemitenteNombre { get; set; } = string.Empty;
    public string Hora { get; set; } = string.Empty;
    public bool Propio { get; set; }
}
