using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DiskUsage.Models;

namespace DiskUsage.Services
{
    public class DiskScannerService : IDiskScannerService
    {
        public async Task<FileSystemItem> ScanDirectoryAsync(
            string rootPath,
            IProgress<ScanProgressReport>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("Path cannot be empty or whitespace.", nameof(rootPath));
            }

            if (!Directory.Exists(rootPath))
            {
                throw new DirectoryNotFoundException($"Directory not found: {rootPath}");
            }

            return await Task.Run(() =>
            {
                var directoryInfo = new DirectoryInfo(rootPath);
                var rootItem = new FileSystemItem
                {
                    Name = string.IsNullOrEmpty(directoryInfo.Name) ? rootPath : directoryInfo.Name,
                    FullPath = directoryInfo.FullName,
                    ItemType = FileSystemItemType.Directory,
                    LastModified = directoryInfo.Exists ? directoryInfo.LastWriteTime : DateTime.MinValue,
                    Extension = "Folder"
                };

                long totalFiles = 0;
                long totalDirs = 0;
                long totalBytes = 0;
                var visitedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Throttle progress updates to avoid UI overhead
                var lastReportTime = DateTime.UtcNow;

                void ReportProgress(string currentPath, string? error = null)
                {
                    var now = DateTime.UtcNow;
                    if ((now - lastReportTime).TotalMilliseconds > 100 || error != null)
                    {
                        lastReportTime = now;
                        progress?.Report(new ScanProgressReport
                        {
                            CurrentPath = currentPath,
                            ScannedFiles = totalFiles,
                            ScannedDirectories = totalDirs,
                            TotalBytes = totalBytes,
                            ErrorMessage = error
                        });
                    }
                }

                ScanNode(rootItem, visitedPaths, ref totalFiles, ref totalDirs, ref totalBytes, ReportProgress, cancellationToken);

                // Final progress report
                progress?.Report(new ScanProgressReport
                {
                    CurrentPath = rootItem.FullPath,
                    ScannedFiles = totalFiles,
                    ScannedDirectories = totalDirs,
                    TotalBytes = totalBytes
                });

                return rootItem;
            }, cancellationToken);
        }

        private void ScanNode(
            FileSystemItem currentItem,
            HashSet<string> visitedPaths,
            ref long totalFiles,
            ref long totalDirs,
            ref long totalBytes,
            Action<string, string?> reportProgress,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var normalizedPath = Path.GetFullPath(currentItem.FullPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!visitedPaths.Add(normalizedPath))
            {
                // Prevent infinite loop on circular symlinks / junction points
                return;
            }

            reportProgress(currentItem.FullPath, null);

            var dirInfo = new DirectoryInfo(currentItem.FullPath);
            if (!dirInfo.Exists)
            {
                return;
            }

            long currentDirTotalSize = 0;
            int directFileCount = 0;
            int totalNestedFiles = 0;
            int totalNestedDirs = 0;

            // 1. Process files
            try
            {
                var files = dirInfo.EnumerateFiles();
                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var fileItem = new FileSystemItem
                        {
                            Name = file.Name,
                            FullPath = file.FullName,
                            ItemType = FileSystemItemType.File,
                            Size = file.Length,
                            FileCount = 1,
                            DirectoryCount = 0,
                            LastModified = file.LastWriteTime,
                            Extension = string.IsNullOrEmpty(file.Extension) ? "File" : file.Extension.ToLowerInvariant(),
                            Parent = currentItem
                        };

                        currentItem.Children.Add(fileItem);
                        currentDirTotalSize += file.Length;
                        directFileCount++;
                        totalNestedFiles++;
                        totalFiles++;
                        totalBytes += file.Length;
                    }
                    catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                    {
                        // Single file access issue
                        var errorFileItem = new FileSystemItem
                        {
                            Name = file.Name,
                            FullPath = file.FullName,
                            ItemType = FileSystemItemType.File,
                            Size = 0,
                            LastModified = DateTime.MinValue,
                            Extension = file.Extension,
                            HasAccessError = true,
                            ErrorMessage = ex.Message,
                            Parent = currentItem
                        };
                        currentItem.Children.Add(errorFileItem);
                    }
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or PathTooLongException or IOException)
            {
                currentItem.HasAccessError = true;
                currentItem.ErrorMessage = ex.Message;
                reportProgress(currentItem.FullPath, ex.Message);
            }

            // 2. Process subdirectories
            try
            {
                var subDirs = dirInfo.EnumerateDirectories();
                foreach (var subDir in subDirs)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Check if reparse point (junction/symlink) and handle carefully
                    bool isReparsePoint = (subDir.Attributes & FileAttributes.ReparsePoint) != 0;

                    var subDirItem = new FileSystemItem
                    {
                        Name = subDir.Name,
                        FullPath = subDir.FullName,
                        ItemType = FileSystemItemType.Directory,
                        LastModified = subDir.LastWriteTime,
                        Extension = isReparsePoint ? "Junction" : "Folder",
                        Parent = currentItem
                    };

                    currentItem.Children.Add(subDirItem);
                    totalDirs++;
                    totalNestedDirs++;

                    if (!isReparsePoint)
                    {
                        ScanNode(subDirItem, visitedPaths, ref totalFiles, ref totalDirs, ref totalBytes, reportProgress, cancellationToken);
                    }

                    currentDirTotalSize += subDirItem.Size;
                    totalNestedFiles += subDirItem.FileCount;
                    totalNestedDirs += subDirItem.DirectoryCount;
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or PathTooLongException or IOException)
            {
                currentItem.HasAccessError = true;
                currentItem.ErrorMessage = ex.Message;
                reportProgress(currentItem.FullPath, ex.Message);
            }

            // Bottom-up rollup size and counts for current directory
            currentItem.Size = currentDirTotalSize;
            currentItem.FileCount = totalNestedFiles;
            currentItem.DirectoryCount = totalNestedDirs;

            // Sort children by Size descending for instant top usage display
            currentItem.Children.Sort((a, b) => b.Size.CompareTo(a.Size));
        }
    }
}
