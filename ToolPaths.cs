namespace ModIt;

public static class ToolPaths
{
    public static string BaseDir => AppContext.BaseDirectory;

    public static string ToolsDir => Path.Combine(BaseDir, "Tools");

    public static string ApktoolJar => Path.Combine(ToolsDir, "apktool.jar");

    public static string JadxBat => Path.Combine(ToolsDir, "jadx", "bin", "jadx.bat");

    public static string BaksmaliJar => Path.Combine(ToolsDir, "baksmali.jar");

    public static string SmaliJar => Path.Combine(ToolsDir, "smali.jar");

    public static string Dex2JarBat => Path.Combine(ToolsDir, "dex2jar", "d2j-dex2jar.bat");

    public static string ApkSignerJar => Path.Combine(ToolsDir, "build-tools", "apksigner.jar");

    public static string ZipAlignExe => Path.Combine(ToolsDir, "build-tools", "zipalign.exe");

    public static string JavaExe
    {
        get
        {
            var jh = Environment.GetEnvironmentVariable("JAVA_HOME");
            if (!string.IsNullOrEmpty(jh))
            {
                var p = Path.Combine(jh, "bin", "java.exe");
                if (File.Exists(p)) return p;
            }
            return "java";
        }
    }

    public static Dictionary<string, string> JavaEnv()
    {
        var jh = Environment.GetEnvironmentVariable("JAVA_HOME");
        var d = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(jh))
            d["JAVA_HOME"] = jh;
        return d;
    }
}