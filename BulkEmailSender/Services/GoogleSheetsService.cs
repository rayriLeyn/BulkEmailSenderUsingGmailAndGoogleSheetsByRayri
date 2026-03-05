using BulkEmailSender.Config;
using BulkEmailSender.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;

namespace BulkEmailSender.Services;

public class GoogleSheetsService
{
    private readonly GoogleSettings _config;

    public GoogleSheetsService(GoogleSettings config)
    {
        _config = config;
    }

    public async Task<List<Recipient>> GetRecipientsAsync()
    {
        var credential = GoogleCredential
            .FromFile(_config.CredentialsPath)
            .CreateScoped(SheetsService.Scope.SpreadsheetsReadonly);

        var service = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "BulkEmailSender"
        });

        var request = service.Spreadsheets.Values.Get(_config.SheetId, "Sheet1!A:D");
        var response = await request.ExecuteAsync();

        if (response.Values == null || response.Values.Count <= 1)
        {
            Console.WriteLine("⚠️  No recipients found. Make sure your sheet has data and a header row.");
            return new List<Recipient>();
        }

        return response.Values
     .Skip(1)
     .Where(row => row.Count > 0 && !string.IsNullOrWhiteSpace(row[0]?.ToString()))
     .Select(row => new Recipient
     {
         Email = row[0].ToString()!.Trim(),
         Name = row.Count > 1 ? row[1].ToString()?.Trim() ?? "" : "",
         Position = row.Count > 2 ? row[2].ToString()?.Trim() ?? "" : "",
         Status = row.Count > 3 ? row[3].ToString()?.Trim() ?? "" : ""
     })
     .ToList();
    }
}