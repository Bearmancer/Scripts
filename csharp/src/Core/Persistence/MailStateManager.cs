namespace CSharpScripts.Core;

internal static class MailStateManager
{
	private static readonly string StateFilePath = Path.Combine(
		Paths.StateDirectory,
		"mail",
		"active.json"
	);

	private static readonly JsonSerializerOptions JsonOptions = StateManager.JsonIndented;

	internal static async Task<ActiveMailbox?> LoadAsync(CancellationToken ct = default)
	{
		ct.ThrowIfCancellationRequested();

		if (!File.Exists(path: StateFilePath))
			return null;

		try
		{
			var json = await File.ReadAllTextAsync(StateFilePath, ct);
			return JsonSerializer.Deserialize<ActiveMailbox>(json: json, options: JsonOptions);
		}
		catch (JsonException ex)
		{
			Log.Warning("Mail state file corrupted: {Message}", ex.Message);
			return null;
		}
	}

	internal static async Task SaveAsync(ActiveMailbox mailbox, CancellationToken ct = default)
	{
		ct.ThrowIfCancellationRequested();

		var directory = Path.GetDirectoryName(path: StateFilePath)!;
		Directory.CreateDirectory(path: directory);

		var json = JsonSerializer.Serialize(value: mailbox, options: JsonOptions);
		var tempPath = StateFilePath + $".{Guid.NewGuid()}.tmp";
		await File.WriteAllTextAsync(tempPath, contents: json, ct);
		File.Move(sourceFileName: tempPath, destFileName: StateFilePath, overwrite: true);

		Log.Debug("Mail state saved: {Address} ({Provider})", mailbox.Address, mailbox.Provider);
	}

	internal static void Delete()
	{
		if (File.Exists(path: StateFilePath))
			File.Delete(path: StateFilePath);

		Log.Debug("Mail state file deleted");
	}
}
