using System;
using System.Threading;
using System.Threading.Tasks;
using DiskUsage.Models;

namespace DiskUsage.Services
{
    public interface IDiskScannerService
    {
        Task<FileSystemItem> ScanDirectoryAsync(
            string rootPath,
            IProgress<ScanProgressReport>? progress = null,
            CancellationToken cancellationToken = default);
    }
}
