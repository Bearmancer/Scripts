namespace CSharpScripts.Models;

internal sealed record ArticleContent
{
	public required string Title { get; init; }
	public IReadOnlyList<string> Authors { get; init; } = [];
	public string Journal { get; init; } = "";
	public string PublicationDate { get; init; } = "";
	public string Doi { get; init; } = "";
	public string AbstractText { get; init; } = "";
	public Uri SourceUrl { get; init; } = new("about:blank");
	public required string BodyHtml { get; init; }
	public Dictionary<string, byte[]> Images { get; init; } = [];
	public byte[]? OriginalPdf { get; init; }
}
