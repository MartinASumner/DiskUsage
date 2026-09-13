using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using DiskUsage.ViewModels;

namespace DiskUsage
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isSyncingTreeView;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            DataContextChanged += MainWindow_DataContextChanged;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            HookViewModel(DataContext as MainViewModel);
        }

        private void MainWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is MainViewModel oldVm)
            {
                oldVm.PropertyChanged -= ViewModel_PropertyChanged;
            }
            HookViewModel(e.NewValue as MainViewModel);
        }

        private void HookViewModel(MainViewModel? vm)
        {
            if (vm != null)
            {
                vm.PropertyChanged -= ViewModel_PropertyChanged;
                vm.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.CurrentFolder))
            {
                if (DataContext is MainViewModel vm && vm.CurrentFolder != null)
                {
                    SelectFolderInTreeView(vm.CurrentFolder);
                }
            }
        }

        private void SelectFolderInTreeView(FileSystemItemViewModel targetFolder)
        {
            Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    _isSyncingTreeView = true;

                    var path = new List<FileSystemItemViewModel>();
                    var current = targetFolder;
                    while (current != null)
                    {
                        path.Add(current);
                        current.IsExpanded = true;
                        current = current.Parent;
                    }
                    path.Reverse();

                    ItemsControl parentContainer = FolderTreeView;
                    TreeViewItem? targetItemContainer = null;

                    foreach (var node in path)
                    {
                        parentContainer.UpdateLayout();
                        var tvi = parentContainer.ItemContainerGenerator.ContainerFromItem(node) as TreeViewItem;
                        if (tvi == null)
                        {
                            if (parentContainer is TreeViewItem pTvi)
                            {
                                pTvi.IsExpanded = true;
                                pTvi.UpdateLayout();
                            }
                            tvi = parentContainer.ItemContainerGenerator.ContainerFromItem(node) as TreeViewItem;
                        }

                        if (tvi != null)
                        {
                            tvi.IsExpanded = true;
                            targetItemContainer = tvi;
                            parentContainer = tvi;
                        }
                    }

                    if (targetItemContainer != null)
                    {
                        targetItemContainer.IsSelected = true;
                        targetItemContainer.BringIntoView();
                    }
                }
                catch
                {
                    // Non-critical UI sync guard
                }
                finally
                {
                    _isSyncingTreeView = false;
                }
            }, DispatcherPriority.Loaded);
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
            if (_isSyncingTreeView)
            {
                return;
            }

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