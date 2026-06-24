namespace VS.Helper.Core.Zip;

internal sealed class ZipBuildResult
{
    public string ZipPath { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public string ConfigPath { get; set; } = string.Empty;
    public bool UsedGeneratedConfig { get; set; }
}
