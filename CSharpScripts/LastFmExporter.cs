using System.Globalization;
using Google;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Hqub.Lastfm;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using Spectre.Console;

namespace CSharpScripts;

public class LastFmExporter
{
    const string TIME_FORMAT = "yyyy/MM/dd HH:mm:ss";
    const int COLUMN_COUNT = 4;
    const string HEADER_RANGE = "A1:D1";
    const int DELAY_MILLISECONDS = 1000;

    static readonly ResiliencePipeline Pipeline = new ResiliencePipelineBuilder()
        .AddRetry(
            new RetryStrategyOptions
            {
                ShouldHandle = static args =>
                    args.Outcome switch
                    {
                        {
                            Exception: GoogleApiException
                                or ServiceException
                                or TimeoutRejectedException
                        } => PredicateResult.True(),
                        _ => PredicateResult.False(),
                    },
                MaxRetryAttempts = Config.MAX_RETRY_ATTEMPTS,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(10),
                OnRetry = static args =>
                {
                    Logger.Warning($"Retry {args.AttemptNumber + 1}/{Config.MAX_RETRY_ATTEMPTS}");
                    return default;
                },
            }
        )
        .AddTimeout(TimeSpan.FromSeconds(Config.REQUEST_TIMEOUT_SECONDS))
        .Build();

    public static async Task Yo()
    {
        Logger.Info(message: "Last.fm Scrobble Exporter", isBold: true);

        var sheetsService = await ExecuteWithRetryAsync(() =>
            GoogleAuth.CreateSheetsServiceAsync(appName: Config.APP_NAME)
        );

        await EnsureHeaderRowAsync(service: sheetsService);

        var lastSyncedDate = await GetLastSyncedDateAsync(service: sheetsService);
        var newTracks = await FetchAllNewScrobblesAsync(
            client: Config.LastFmClient,
            afterDate: lastSyncedDate
        );

        if (newTracks.Count == 0)
        {
            Logger.Info("No new scrobbles to sync");
            return;
        }

        DisplaySyncSummary(tracks: newTracks, lastSyncedDate: lastSyncedDate);
        await PrependTracksToSheetAsync(service: sheetsService, tracks: newTracks);
    }

    static async Task EnsureHeaderRowAsync(SheetsService service)
    {
        var response = await GoogleSheets.GetAsync(
            service: service,
            spreadsheetId: Config.SPREADSHEET_ID,
            rangeA1: $"{Config.SHEET_NAME}!{HEADER_RANGE}"
        );

        if (response?.Values?.Count > 0)
            return;

        await GoogleSheets.UpdateAsync(
            service: service,
            spreadsheetId: Config.SPREADSHEET_ID,
            rangeA1: $"{Config.SHEET_NAME}!{HEADER_RANGE}",
            rows:
            [
                ["Time", "Title", "Artist", "Album"],
            ]
        );

        var sheetId = await GoogleSheets.GetSheetIdAsync(
            service: service,
            spreadsheetId: Config.SPREADSHEET_ID,
            sheetName: Config.SHEET_NAME
        );

        await GoogleSheets.FormatHeaderRowAsync(
            service: service,
            spreadsheetId: Config.SPREADSHEET_ID,
            sheetId: sheetId,
            columnCount: COLUMN_COUNT
        );

        Logger.Info("Header row created");
    }

    static async Task<DateTime?> GetLastSyncedDateAsync(SheetsService service)
    {
        return await ExecuteWithRetryAsync<DateTime?>(async () =>
        {
            var response = await GoogleSheets.GetAsync(
                service: service,
                spreadsheetId: Config.SPREADSHEET_ID,
                rangeA1: $"{Config.SHEET_NAME}!A2"
            );

            var value = response?.Values?.FirstOrDefault()?[0]?.ToString();
            if (string.IsNullOrEmpty(value))
                return null;

            if (
                !DateTime.TryParseExact(
                    s: value,
                    format: TIME_FORMAT,
                    provider: CultureInfo.InvariantCulture,
                    style: DateTimeStyles.None,
                    result: out var parsedDate
                )
            )
                throw new FormatException($"Invalid date format in sheet: '{value}'");

            if (parsedDate > DateTime.Now)
                throw new InvalidOperationException(
                    $"Last synced date is in the future: {parsedDate}"
                );

            return parsedDate.AddSeconds(1);
        });
    }

    static async Task<List<Scrobble>> FetchAllNewScrobblesAsync(
        LastfmClient client,
        DateTime? afterDate
    )
    {
        var page = 1;
        List<Scrobble> collected = [];

        while (true)
        {
            var response = await ExecuteWithRetryAsync(() =>
                client.User.GetRecentTracksAsync(user: Config.USERNAME, from: afterDate, page: page)
            );

            var tracks = response
                .Items.Where(track => track.Date.HasValue)
                .Select(track => new Scrobble(
                    ScrobbleTime: track.Date!.Value,
                    TrackName: track.Name,
                    ArtistName: track.Artist?.Name ?? string.Empty,
                    AlbumName: track.Album?.Name ?? string.Empty
                ))
                .ToList();

            if (tracks.Count == 0)
                break;

            if (!afterDate.HasValue)
            {
                collected.AddRange(tracks);
                break;
            }

            if (tracks.Last().ScrobbleTime < afterDate.Value)
            {
                collected.AddRange(tracks.Where(track => track.ScrobbleTime >= afterDate.Value));
                break;
            }

            collected.AddRange(tracks);
            page++;
            await Task.Delay(DELAY_MILLISECONDS);
        }

        return [.. collected.OrderByDescending(track => track.ScrobbleTime)];
    }

    static async Task PrependTracksToSheetAsync(SheetsService service, List<Scrobble> tracks)
    {
        await ExecuteWithRetryAsync(async () =>
        {
            var sheetId = await GoogleSheets.GetSheetIdAsync(
                service: service,
                spreadsheetId: Config.SPREADSHEET_ID,
                sheetName: Config.SHEET_NAME
            );

            await GoogleSheets.InsertRowsAsync(
                service: service,
                spreadsheetId: Config.SPREADSHEET_ID,
                sheetId: sheetId,
                startIndex: 1,
                rowCount: tracks.Count
            );

            var rows = tracks
                .Select(track =>
                    (IList<object>)
                        [
                            track.ScrobbleTime.ToString(TIME_FORMAT),
                            track.TrackName,
                            track.ArtistName,
                            track.AlbumName,
                        ]
                )
                .ToList();

            await GoogleSheets.UpdateAsync(
                service: service,
                spreadsheetId: Config.SPREADSHEET_ID,
                rangeA1: $"{Config.SHEET_NAME}!A2:D{1 + tracks.Count}",
                rows: rows
            );

            return true;
        });

        Logger.Info($"Pushed {tracks.Count} scrobbles to Sheets");
    }

    static void DisplaySyncSummary(List<Scrobble> tracks, DateTime? lastSyncedDate)
    {
        var delimiter = new string('=', 70);

        Logger.Info("\n" + delimiter);
        Logger.Info(message: "SYNC SUMMARY", isBold: true);
        Logger.Info(delimiter);

        if (lastSyncedDate.HasValue && tracks.Count > 0)
        {
            var lastTrack = tracks.Last();
            Logger.Info($"\nLast synced: {lastTrack.ScrobbleTime.ToString(TIME_FORMAT)}");
            Logger.Info($"  {lastTrack.TrackName} / {lastTrack.ArtistName}");

            var latestTrack = tracks.First();
            Logger.Info($"\nLatest on Last.fm: {latestTrack.ScrobbleTime.ToString(TIME_FORMAT)}");
            Logger.Info($"  {latestTrack.TrackName} / {latestTrack.ArtistName}");
        }

        Logger.Info($"\nScrobbles synced: {tracks.Count}");
        Logger.Info(delimiter + "\n");
    }

    static async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action) =>
        await Pipeline.ExecuteAsync(async token => await action(), CancellationToken.None);

    sealed record Scrobble(
        DateTime ScrobbleTime,
        string TrackName,
        string ArtistName,
        string AlbumName
    );
}

public static class Config
{
    public const string USERNAME = "kanishknishar";
    public const string APP_NAME = "LastFmExporter";
    public const string SPREADSHEET_ID = "1ZyxCmHQCrCfyQP29_eFiuLsWleyXhX2MprNOdmPat50";
    public const string SHEET_NAME = "Scrobbles";
    public const int MAX_RETRY_ATTEMPTS = 10;
    public const int REQUEST_TIMEOUT_SECONDS = 30;
    public const string CREDENTIAL_PATH = @"C:\Users\Lance\AppData\Local\Personal\Scripts";

    public static readonly LastfmClient LastFmClient = new(
        apiKey: LastFmApiKey,
        apiSecret: LastFmApiSecret
    );

    public static string LastFmApiKey =>
        Environment.GetEnvironmentVariable("LASTFM_API_KEY")
        ?? throw new InvalidOperationException("LASTFM_API_KEY environment variable not set");

    public static string LastFmApiSecret =>
        Environment.GetEnvironmentVariable("LASTFM_API_SECRET")
        ?? throw new InvalidOperationException("LASTFM_API_SECRET environment variable not set");
}
