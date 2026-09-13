namespace DiskUsage.Models
{
    public class ScanProgressReport
    {
        public string CurrentPath { get; set; } = string.Empty;
        public long ScannedFiles { get; set; }
        public long ScannedDirectories { get; set; }
        public long TotalBytes { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
