namespace CSharpScripts.Infrastructure;

internal static class StateManager
{
    static readonly string ProjectRoot = GetDirectoryName(
        GetDirectoryName(GetDirectoryName(GetDirectoryName(AppContext.BaseDirectory)!)!)!
    )!;
    static readonly string StateDirectory = Combine(ProjectRoot, "state");

    internal const string ScrobblesFile = "scrobbles.json";
    internal const string FetchStateFile = "scrobblefetchstate.json";
    internal const string YouTubeStateFile = "youtubefetchstate.json";

    internal static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    internal static T Load<T>(string fileName)
        where T : class, new()
    {
        CreateDirectory(StateDirectory);
        var path = GetPath(fileName);
        if (!File.Exists(path))
            return new T();

        var json = ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? new T();
    }

    internal static void Save<T>(string fileName, T state)
    {
        CreateDirectory(StateDirectory);
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
        foreach (var file in GetFiles(StateDirectory, "playlist_*.json"))
            File.Delete(file);
        Logger.Debug("Deleted YouTube state files");
    }

    static string GetPath(string fileName) =>
        Combine(StateDirectory, fileName.EndsWith(".json") ? fileName : $"{fileName}.json");
}
