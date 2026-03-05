namespace BulkEmailSender.Models;

public class Recipient
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public string Position { get; set; } = string.Empty;  
    public string Status { get; set; } = string.Empty;
}