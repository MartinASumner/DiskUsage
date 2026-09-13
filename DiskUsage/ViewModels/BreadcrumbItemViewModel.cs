using CommunityToolkit.Mvvm.ComponentModel;

namespace DiskUsage.ViewModels
{
    public partial class BreadcrumbItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _fullPath = string.Empty;

        public FileSystemItemViewModel Node { get; }

        public BreadcrumbItemViewModel(string name, string fullPath, FileSystemItemViewModel node)
        {
            Name = name;
            FullPath = fullPath;
            Node = node;
        }
    }
}
