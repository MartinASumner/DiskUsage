using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DiskUsage.ViewModels;

namespace DiskUsage
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem tvi)
            {
                tvi.BringIntoView();
                e.Handled = true;
            }
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is MainViewModel vm && e.NewValue is FileSystemItemViewModel selectedFolder)
            {
                if (selectedFolder.IsDirectory && vm.CurrentFolder != selectedFolder)
                {
                    vm.NavigateToFolder(selectedFolder);
                }
            }
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.SelectedItem != null && vm.SelectedItem.IsDirectory)
            {
                vm.NavigateToFolder(vm.SelectedItem);
            }
        }
    }
}