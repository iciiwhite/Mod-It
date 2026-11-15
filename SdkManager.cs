namespace ModIt;

public static class SdkManager
{
    public static (bool Ok, string Message) Validate(string sdkPath)
    {
        if (string.IsNullOrWhiteSpace(sdkPath) || !Directory.Exists(sdkPath))
            return (false, "SDK path does not exist.");

        var buildTools = Path.Combine(sdkPath, "build-tools");
        if (!Directory.Exists(buildTools))
            return (false, "build-tools folder not found.");

        var versions = Directory.GetDirectories(buildTools)
            .Select(Path.GetFileName)
            .Where(v => !string.IsNullOrEmpty(v))
            .ToList();

        return versions.Count > 0
            ? (true, $"Found build-tools: {string.Join(", ", versions)}")
            : (false, "No build-tools versions installed.");
    }
}