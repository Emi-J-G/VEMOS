namespace VEMOS.Models;

public class FiltroEditorViewModel
{
    public string Nombre { get; set; } = string.Empty;
    public bool Seleccionado { get; set; }
    public string ClaseSeleccionado { get; set; } = string.Empty;
    public string AriaPressed { get; set; } = "false";
    public string TextoCheck { get; set; } = "+";
}
