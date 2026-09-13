using System;

namespace DiskUsage.Helpers
{
    public static class ByteSizeFormatter
    {
        private static readonly string[] SizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };

        public static string Format(long bytes)
        {
            if (bytes < 0)
            {
                return "0 B";
            }

            if (bytes == 0)
            {
                return "0 B";
            }

            int index = 0;
            double dBytes = bytes;

            while (dBytes >= 1024 && index < SizeSuffixes.Length - 1)
            {
                dBytes /= 1024;
                index++;
            }

            return index == 0 ? $"{dBytes:0} {SizeSuffixes[index]}" : $"{dBytes:0.##} {SizeSuffixes[index]}";
        }
    }
}
