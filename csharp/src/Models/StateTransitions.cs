// (removed pragma IDE0007, IDE0028; will apply explicit fixes for var/type and collection creation)

namespace CSharpScripts.Models;

internal static class StateTransitions
{
	public static FetchState Reset(string spreadsheetId) => new() { SpreadsheetId = spreadsheetId };

	extension(YouTubeFetchState state)
	{
		public YouTubeFetchState RefreshTimestamps() =>
			state with
			{
				LastChecked = DateTime.UtcNow,
				LastUpdated = DateTime.UtcNow,
			};

		public YouTubeFetchState WithSpreadsheetId(string spreadsheetId) =>
			state with
			{
				SpreadsheetId = spreadsheetId,
			};

		public YouTubeFetchState MarkFetchComplete() => state with { FetchComplete = true };

		public YouTubeFetchState WithPlaylistSnapshot(string playlistId, PlaylistSnapshot snapshot)
		{
			var updated = new Dictionary<string, PlaylistSnapshot>(state.PlaylistSnapshots)
			{
				[playlistId] = snapshot,
			};
			return state with { PlaylistSnapshots = updated, LastUpdated = DateTime.UtcNow };
		}

		public YouTubeFetchState RemovePlaylistSnapshot(string playlistId)
		{
			var updated = new Dictionary<string, PlaylistSnapshot>(state.PlaylistSnapshots);
			updated.Remove(playlistId);
			return state with { PlaylistSnapshots = updated, LastUpdated = DateTime.UtcNow };
		}
	}

	extension(FetchState state)
	{
		public FetchState MarkFetchComplete() => state with { FetchComplete = true };

		public FetchState WithSpreadsheetId(string spreadsheetId) =>
			state with
			{
				SpreadsheetId = spreadsheetId,
			};
	}
}
