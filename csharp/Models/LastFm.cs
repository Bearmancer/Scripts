namespace CSharpScripts.Models;

internal record Scrobble(string TrackName, string ArtistName, string AlbumName, DateTime? PlayedAt)
{
    internal string FormattedDate => PlayedAt?.ToString("yyyy/MM/dd HH:mm:ss") ?? "";
}

internal record FetchState
{
    public int LastPage { get; set; }
    public int TotalFetched { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.Now;
    public string? SpreadsheetId { get; set; }
    public bool FetchComplete { get; set; }

    internal void Update(int page, int total)
    {
        LastPage = page;
        TotalFetched = total;
        LastUpdated = DateTime.Now;
    }
}
