using System.Text.Encodings.Web;

namespace CSharpScripts.Core;

internal static class MailStateManager
{
	private static readonly string StateFilePath = Path.Combine(
		Paths.StateDirectory,
		"mail",
		"active.json"
	);

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
	};

	internal static async Task<ActiveMailbox?> LoadAsync(CancellationToken ct = default)
	{
		ct.ThrowIfCancellationRequested();

		if (!File.Exists(StateFilePath))
			return null;

		try
		{
			var json = await File.ReadAllTextAsync(StateFilePath, ct);
			return JsonSerializer.Deserialize<ActiveMailbox>(json, JsonOptions);
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

		var directory = Path.GetDirectoryName(StateFilePath)!;
		Directory.CreateDirectory(directory);

		var json = JsonSerializer.Serialize(mailbox, JsonOptions);
		var tempPath = StateFilePath + $".{Guid.NewGuid()}.tmp";
		await File.WriteAllTextAsync(tempPath, json, ct);
		File.Move(tempPath, StateFilePath, overwrite: true);

		Log.Debug("Mail state saved: {Address} ({Provider})", mailbox.Address, mailbox.Provider);
	}

	internal static void Delete()
	{
		if (File.Exists(StateFilePath))
			File.Delete(StateFilePath);

		Log.Debug("Mail state file deleted");
	}
}
