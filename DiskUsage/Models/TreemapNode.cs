using System.Windows;
using System.Windows.Media;
using DiskUsage.Helpers;
using DiskUsage.ViewModels;

namespace DiskUsage.Models
{
    public class TreemapNode
    {
        public FileSystemItemViewModel Item { get; }
        public Rect Bounds { get; set; }
        public Color Color { get; }
        public Brush FillBrush { get; }
        public FileCategory Category { get; }

        public string Name => Item.Name;
        public string FormattedSize => Item.FormattedSize;
        public string PercentOfParentText => Item.PercentOfParentText;
        public string FullPath => Item.FullPath;
        public bool IsDirectory => Item.IsDirectory;

        public bool IsLargeEnoughForText => Bounds.Width >= 40 && Bounds.Height >= 24;
        public bool IsLargeEnoughForFullDetails => Bounds.Width >= 80 && Bounds.Height >= 45;

        public TreemapNode(FileSystemItemViewModel item, Rect bounds)
        {
            Item = item;
            Bounds = bounds;
            Category = FileCategoryHelper.Categorize(item.Model);
            Color = FileCategoryHelper.GetCategoryColor(Category);

            var brush = new SolidColorBrush(Color);
            brush.Freeze();
            FillBrush = brush;
        }
    }
}
