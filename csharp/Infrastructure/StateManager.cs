namespace CSharpScripts.Infrastructure;

internal static class StateManager
{
    internal const string ScrobblesFile = "scrobbles.json";
    internal const string FetchStateFile = "scrobblefetchstate.json";
    internal const string YouTubeStateFile = "youtubefetchstate.json";

    internal static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    internal static T Load<T>(string fileName)
        where T : class, new()
    {
        CreateDirectory(Paths.StateDirectory);
        var path = GetPath(fileName);
        if (!File.Exists(path))
            return new T();

        var json = ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? new T();
    }

    internal static void Save<T>(string fileName, T state)
    {
        CreateDirectory(Paths.StateDirectory);
        WriteAllText(GetPath(fileName), JsonSerializer.Serialize(state, JsonOptions));
    }

    internal static void Delete(string fileName)
    {
        var path = GetPath(fileName);
        if (File.Exists(path))
            File.Delete(path);
    }

    internal static void DeleteLastFmStates()
    {
        Delete(FetchStateFile);
        Delete(ScrobblesFile);
        Logger.Debug("Deleted Last.fm state files");
    }

    internal static void DeleteYouTubeStates()
    {
        Delete(YouTubeStateFile);
        foreach (var file in GetFiles(Paths.StateDirectory, "playlist_*.json"))
            File.Delete(file);
        Logger.Debug("Deleted YouTube state files");
    }

    static string GetPath(string fileName) =>
        Combine(Paths.StateDirectory, fileName.EndsWith(".json") ? fileName : $"{fileName}.json");
}
