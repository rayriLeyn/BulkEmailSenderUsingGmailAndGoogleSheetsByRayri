namespace BulkEmailSender.Config;

public class AppSettings
{
    public GmailSettings Gmail { get; set; } = new();
    public GoogleSettings Google { get; set; } = new();
}

public class GmailSettings
{
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string AppPassword { get; set; } = string.Empty;
}

public class GoogleSettings
{
    public string SheetId { get; set; } = string.Empty;
    public string CredentialsPath { get; set; } = string.Empty;
}