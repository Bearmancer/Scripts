namespace CSharpScripts.Core;

internal static class MailStateManager
{
	private static readonly string StateFilePath = Path.Combine(
		path1: Paths.StateDirectory,
		path2: "mail",
		path3: "active.json"
	);

	private static readonly JsonSerializerOptions JsonOptions = StateManager.JsonIndented;

	internal static async Task<ActiveMailbox?> LoadAsync(CancellationToken ct = default)
	{
		ct.ThrowIfCancellationRequested();

		if (!File.Exists(path: StateFilePath))
			return null;

		try
		{
			var json = await File.ReadAllTextAsync(path: StateFilePath, cancellationToken: ct);
			return JsonSerializer.Deserialize<ActiveMailbox>(json: json, options: JsonOptions);
		}
		catch (JsonException ex)
		{
			Log.Error(
				ex: ex,
				messageTemplate: "Could not load MailStateManager: {Message}",
				ex.Message
			);
			return new MailState();
		}
	}

	internal static async Task SaveAsync(ActiveMailbox mailbox, CancellationToken ct = default)
	{
		ct.ThrowIfCancellationRequested();

		var directory = Path.GetDirectoryName(path: StateFilePath)!;
		Directory.CreateDirectory(path: directory);

		var json = JsonSerializer.Serialize(value: mailbox, options: JsonOptions);
		var tempPath = StateFilePath + $".{Guid.NewGuid()}.tmp";
		await File.WriteAllTextAsync(path: tempPath, contents: json, cancellationToken: ct);
		File.Move(sourceFileName: tempPath, destFileName: StateFilePath, overwrite: true);

		Log.Debug(
			messageTemplate: "Mail state saved: {Address} ({Provider})",
			mailbox.Address,
			mailbox.Provider
		);
	}

	internal static void Delete()
	{
		if (File.Exists(path: StateFilePath))
			File.Delete(path: StateFilePath);

		Log.Debug(messageTemplate: "Mail state file deleted");
	}
}
