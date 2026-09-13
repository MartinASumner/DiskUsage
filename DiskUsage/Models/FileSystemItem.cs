using System;
using System.Collections.Generic;

namespace DiskUsage.Models
{
    public enum FileSystemItemType
    {
        Directory,
        File
    }

    public class FileSystemItem
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public FileSystemItemType ItemType { get; set; }
        public long Size { get; set; }
        public int FileCount { get; set; }
        public int DirectoryCount { get; set; }
        public DateTime LastModified { get; set; }
        public string Extension { get; set; } = string.Empty;
        public bool HasAccessError { get; set; }
        public string? ErrorMessage { get; set; }

        public FileSystemItem? Parent { get; set; }
        public List<FileSystemItem> Children { get; set; } = new();

        public bool IsDirectory => ItemType == FileSystemItemType.Directory;
        public bool IsFile => ItemType == FileSystemItemType.File;
    }
}
