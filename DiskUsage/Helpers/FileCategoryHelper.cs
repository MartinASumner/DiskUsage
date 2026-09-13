using System;
using System.Collections.Generic;
using System.Windows.Media;
using DiskUsage.Models;

namespace DiskUsage.Helpers
{
    public enum FileCategory
    {
        Directory,
        Media,
        Archive,
        Code,
        Image,
        Executable,
        Document,
        Other
    }

    public static class FileCategoryHelper
    {
        private static readonly HashSet<string> MediaExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v",
            ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a"
        };

        private static readonly HashSet<string> ArchiveExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".iso", ".img", ".cab", ".dmg"
        };

        private static readonly HashSet<string> CodeExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".cpp", ".c", ".h", ".hpp", ".js", ".ts", ".jsx", ".tsx",
            ".py", ".java", ".go", ".rs", ".rb", ".php", ".html", ".css", ".scss",
            ".json", ".xml", ".yml", ".yaml", ".sql", ".sh", ".ps1", ".bat", ".cmd"
        };

        private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".svg", ".webp", ".ico", ".tiff", ".tif", ".psd", ".raw"
        };

        private static readonly HashSet<string> ExecutableExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".dll", ".sys", ".bin", ".msi", ".com", ".dylib", ".so"
        };

        private static readonly HashSet<string> DocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".docx", ".doc", ".xlsx", ".xls", ".pptx", ".ppt", ".txt", ".md", ".rtf", ".csv", ".epub"
        };

        public static FileCategory Categorize(FileSystemItem item)
        {
            if (item.IsDirectory)
            {
                return FileCategory.Directory;
            }

            var ext = item.Extension?.Trim() ?? string.Empty;
            if (!ext.StartsWith('.'))
            {
                ext = "." + ext;
            }

            if (MediaExtensions.Contains(ext)) return FileCategory.Media;
            if (ArchiveExtensions.Contains(ext)) return FileCategory.Archive;
            if (CodeExtensions.Contains(ext)) return FileCategory.Code;
            if (ImageExtensions.Contains(ext)) return FileCategory.Image;
            if (ExecutableExtensions.Contains(ext)) return FileCategory.Executable;
            if (DocumentExtensions.Contains(ext)) return FileCategory.Document;

            return FileCategory.Other;
        }

        public static Color GetCategoryColor(FileCategory category)
        {
            return category switch
            {
                FileCategory.Directory => Color.FromRgb(70, 90, 120),       // Slate blue
                FileCategory.Media => Color.FromRgb(155, 89, 182),          // Rich purple
                FileCategory.Archive => Color.FromRgb(230, 126, 34),        // Bright orange
                FileCategory.Code => Color.FromRgb(41, 128, 185),           // Strong blue
                FileCategory.Image => Color.FromRgb(39, 174, 96),           // Emerald green
                FileCategory.Executable => Color.FromRgb(231, 76, 60),      // Coral red
                FileCategory.Document => Color.FromRgb(22, 160, 133),       // Persian teal
                _ => Color.FromRgb(127, 140, 141)                           // Grayish slate
            };
        }
    }
}
