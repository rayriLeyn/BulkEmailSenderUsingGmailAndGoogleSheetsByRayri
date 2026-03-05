using BulkEmailSender.Services;
using BulkEmailSender.Config;
using BulkEmailSender.Services;
using Microsoft.Extensions.Configuration;

// --------------------------------------------------
// 1. Load configuration
// --------------------------------------------------
var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

var settings = new AppSettings();
config.Bind(settings);

// --------------------------------------------------
// 2. Validate credentials
// --------------------------------------------------
var errors = new List<string>();

if (string.IsNullOrWhiteSpace(settings.Gmail.SenderEmail))
    errors.Add("  - Gmail:SenderEmail is missing");
if (string.IsNullOrWhiteSpace(settings.Gmail.AppPassword))
    errors.Add("  - Gmail:AppPassword is missing");
if (string.IsNullOrWhiteSpace(settings.Google.SheetId))
    errors.Add("  - Google:SheetId is missing");
if (string.IsNullOrWhiteSpace(settings.Google.CredentialsPath))
    errors.Add("  - Google:CredentialsPath is missing");
if (!File.Exists(settings.Google.CredentialsPath))
    errors.Add($"  - credentials.json not found at: {settings.Google.CredentialsPath}");

if (errors.Count > 0)
{
    Console.WriteLine("❌ Cannot start. The following values are missing:");
    errors.ForEach(Console.WriteLine);
    Console.WriteLine("\n👉 Fill in your appsettings.Development.json and try again.");
    return;
}

// --------------------------------------------------
// 3. Load email templates
// --------------------------------------------------
var memberTemplatePath = Path.Combine("Templates", "email_template.html");
var otherTemplatePath = Path.Combine("Templates", "email_other.html");

if (!File.Exists(memberTemplatePath))
{
    Console.WriteLine("❌ email_template.html not found in Templates/");
    return;
}
if (!File.Exists(otherTemplatePath))
{
    Console.WriteLine("❌ email_other.html not found in Templates/");
    return;
}

var memberTemplate = await File.ReadAllTextAsync(memberTemplatePath);
var otherTemplate = await File.ReadAllTextAsync(otherTemplatePath);
Console.WriteLine("📧 Templates loaded.");

// --------------------------------------------------
// 4. Fetch recipients
// --------------------------------------------------
var sheetsService = new GoogleSheetsService(settings.Google);
var recipients = await sheetsService.GetRecipientsAsync();

if (recipients.Count == 0)
{
    Console.WriteLine("⚠️  No recipients found.");
    return;
}

Console.WriteLine($"📋 {recipients.Count} recipient(s) found. Starting send...\n");

// --------------------------------------------------
// 5. Send emails
// --------------------------------------------------
var gmail = new GmailSmtpService(settings.Gmail);
var sentCount = 0;
var skippedCount = 0;

foreach (var recipient in recipients)
{
    // ── Column D check: skip if already sent ──
    if (recipient.Status.Equals("Sent", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"⏭️  Skipped → {recipient.Email} (already sent)");
        skippedCount++;
        continue;
    }

    // ── Column C check: pick template based on position ──
    var template = recipient.Position.Equals("Member", StringComparison.OrdinalIgnoreCase)
        ? memberTemplate
        : otherTemplate;

    var templateLabel = recipient.Position.Equals("Member", StringComparison.OrdinalIgnoreCase)
        ? "member template"
        : "other template";

    try
    {
        await gmail.SendAsync(recipient, template);
        Console.WriteLine($"✅ Sent [{templateLabel}] → {recipient.Email} ({recipient.Position})");
        sentCount++;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Failed → {recipient.Email} | {ex.Message}");
    }

    await Task.Delay(1500);
}

Console.WriteLine($"\n🎉 Done. Sent: {sentCount} | Skipped: {skippedCount} | Total: {recipients.Count}");