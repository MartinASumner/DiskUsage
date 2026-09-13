using System.Collections.Generic;
using System.Windows;
using DiskUsage.Models;
using DiskUsage.ViewModels;

namespace DiskUsage.Services
{
    public interface ITreemapLayoutEngine
    {
        IReadOnlyList<TreemapNode> ComputeLayout(IReadOnlyList<FileSystemItemViewModel> items, Rect containerBounds);
    }
}
