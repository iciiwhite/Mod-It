using System.IO.Compression;

namespace ModIt;

public static class ApkService
{
    public static async Task DecompileAsync(string apk, string outDir, Action<string> log, CancellationToken ct = default)
    {
        var args = $"-jar \"{ToolPaths.ApktoolJar}\" d -f -o \"{outDir}\" \"{apk}\"";
        await ProcessRunner.RunAsync(ToolPaths.JavaExe, args, log, log, ct, ToolPaths.JavaEnv());
    }

    public static async Task RepackAsync(string projectDir, string outApk, Action<string> log, CancellationToken ct = default)
    {
        var args = $"-jar \"{ToolPaths.ApktoolJar}\" b \"{projectDir}\" -o \"{outApk}\"";
        await ProcessRunner.RunAsync(ToolPaths.JavaExe, args, log, log, ct, ToolPaths.JavaEnv());
    }

    public static async Task AlignAsync(string apk, string outApk, Action<string> log, CancellationToken ct = default)
    {
        var args = $"-f 4 \"{apk}\" \"{outApk}\"";
        await ProcessRunner.RunAsync(ToolPaths.ZipAlignExe, args, log, log, ct);
    }

    public static async Task SignAsync(string apk, string keystore, string alias, string storePass, string keyPass, string outApk, Action<string> log, CancellationToken ct = default)
    {
        var args = $"sign --ks \"{keystore}\" --ks-key-alias \"{alias}\" --ks-pass pass:{storePass} --key-pass pass:{keyPass} --out \"{outApk}\" \"{apk}\"";
        await ProcessRunner.RunAsync(ToolPaths.JavaExe, $"-jar \"{ToolPaths.ApkSignerJar}\" {args}", log, log, ct, ToolPaths.JavaEnv());
    }

    public static async Task DexToJavaAsync(string input, string outDir, Action<string> log, CancellationToken ct = default)
    {
        var args = $"-d \"{outDir}\" \"{input}\"";
        await ProcessRunner.RunAsync(ToolPaths.JadxBat, args, log, log, ct, ToolPaths.JavaEnv());
    }

    public static async Task ExportGradleAsync(string input, string outDir, Action<string> log, CancellationToken ct = default)
    {
        var args = $"-e -d \"{outDir}\" \"{input}\"";
        await ProcessRunner.RunAsync(ToolPaths.JadxBat, args, log, log, ct, ToolPaths.JavaEnv());
    }

    public static void SwapFile(string apkPath, string sourceFile, string targetPathInApk)
    {
        using var zip = ZipFile.Open(apkPath, ZipArchiveMode.Update);
        var entry = zip.GetEntry(targetPathInApk);
        entry?.Delete();
        zip.CreateEntryFromFile(sourceFile, targetPathInApk, CompressionLevel.Optimal);
    }

    public static string MakeModFileName(string originalApk)
    {
        var name = Path.GetFileNameWithoutExtension(originalApk);
        return $"mod_{name}.apk";
    }
}