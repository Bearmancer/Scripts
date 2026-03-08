#pragma warning disable IDE0007, IDE0028

namespace CSharpScripts.Models;

internal static class StateTransitions
{
	public static FetchState MarkFetchComplete(this FetchState state) =>
		state with
		{
			FetchComplete = true,
		};

	public static FetchState WithSpreadsheetId(this FetchState state, string spreadsheetId) =>
		state with
		{
			SpreadsheetId = spreadsheetId,
		};

	public static FetchState Reset(string spreadsheetId) => new() { SpreadsheetId = spreadsheetId };

	public static YouTubeFetchState RefreshTimestamps(this YouTubeFetchState state) =>
		state with
		{
			LastChecked = DateTime.UtcNow,
			LastUpdated = DateTime.UtcNow,
		};

	public static YouTubeFetchState WithSpreadsheetId(
		this YouTubeFetchState state,
		string spreadsheetId
	) => state with { SpreadsheetId = spreadsheetId };

	public static YouTubeFetchState MarkFetchComplete(this YouTubeFetchState state) =>
		state with
		{
			FetchComplete = true,
		};

	public static YouTubeFetchState WithPlaylistSnapshot(
		this YouTubeFetchState state,
		string playlistId,
		PlaylistSnapshot snapshot
	)
	{
		Dictionary<string, PlaylistSnapshot> updated = new(state.PlaylistSnapshots)
		{
			[playlistId] = snapshot,
		};
		return state with { PlaylistSnapshots = updated, LastUpdated = DateTime.UtcNow };
	}

	public static YouTubeFetchState RemovePlaylistSnapshot(
		this YouTubeFetchState state,
		string playlistId
	)
	{
		Dictionary<string, PlaylistSnapshot> updated = new(state.PlaylistSnapshots);
		updated.Remove(playlistId);
		return state with { PlaylistSnapshots = updated, LastUpdated = DateTime.UtcNow };
	}
}
