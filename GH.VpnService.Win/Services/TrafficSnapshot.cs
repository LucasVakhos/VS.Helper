namespace GH.VpnService.Win.Services;

public sealed record TrafficSnapshot(
    string TunnelName,
    string Status,
    long RxBytes,
    long TxBytes,
    double DownloadBytesPerSecond,
    double UploadBytesPerSecond,
    TimeSpan Uptime,
    DateTimeOffset CheckedAt)
{
    public string DownloadText => FormatSpeed(DownloadBytesPerSecond);
    public string UploadText => FormatSpeed(UploadBytesPerSecond);
    public string RxText => FormatBytes(RxBytes);
    public string TxText => FormatBytes(TxBytes);

    public static string FormatSpeed(double bytesPerSecond) => FormatBytes((long)Math.Max(0, bytesPerSecond)) + "/s";

    public static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = Math.Max(0, bytes);
        var unit = 0;

        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit == 0 ? $"{value:0} {units[unit]}" : $"{value:0.0} {units[unit]}";
    }
}
