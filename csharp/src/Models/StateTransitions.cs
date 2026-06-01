namespace Scripts.Models;

internal static class StateTransitions
{
	public static FetchState Reset(string spreadsheetId) => new() { SpreadsheetId = spreadsheetId };

	extension(YouTubeFetchState state)
	{
		public YouTubeFetchState RefreshTimestamps() =>
			state with
			{
				LastChecked = DateTimeOffset.UtcNow,
				LastUpdated = DateTimeOffset.UtcNow,
			};

		public YouTubeFetchState WithSpreadsheetId(string spreadsheetId) =>
			state with
			{
				SpreadsheetId = spreadsheetId,
			};

		public YouTubeFetchState MarkFetchComplete() => state with { FetchComplete = true };

		public YouTubeFetchState WithPlaylistSnapshot(string playlistId, PlaylistSnapshot snapshot)
		{
			Dictionary<string, PlaylistSnapshot> updated = new(dictionary: state.PlaylistSnapshots)
			{
				[key: playlistId] = snapshot,
			};
			return state with { PlaylistSnapshots = updated, LastUpdated = DateTimeOffset.UtcNow };
		}

		public YouTubeFetchState RemovePlaylistSnapshot(string playlistId)
		{
			Dictionary<string, PlaylistSnapshot> updated = new(state.PlaylistSnapshots);
			updated.Remove(key: playlistId);
			return state with { PlaylistSnapshots = updated, LastUpdated = DateTimeOffset.UtcNow };
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
