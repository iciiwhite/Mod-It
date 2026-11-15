namespace ModIt;

public static class SourceMaker
{
    public static async Task GenerateAsync(string apk, string outputDir, Action<string> log, CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDir);
        await ApkService.ExportGradleAsync(apk, outputDir, log, ct);
    }
}