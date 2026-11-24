namespace VEMOS.Models;

public class ErrorViewModel
{
    public string RequestId { get; set; } = string.Empty;

    public bool ShowRequestId
    {
        get
        {
            if (string.IsNullOrEmpty(RequestId))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
