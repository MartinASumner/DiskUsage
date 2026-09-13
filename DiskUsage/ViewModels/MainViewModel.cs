using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskUsage.Helpers;
using DiskUsage.Models;
using DiskUsage.Services;
using Microsoft.Win32;

namespace DiskUsage.ViewModels
{
    public enum ContentViewMode
    {
        Split,
        DetailsGrid,
        VisualTreemap
    }

    public partial class MainViewModel : ObservableObject
    {
        private readonly IDiskScannerService _scannerService;
        private CancellationTokenSource? _scanCts;

        private readonly Stack<FileSystemItemViewModel> _backHistory = new();
        private readonly Stack<FileSystemItemViewModel> _forwardHistory = new();

        [ObservableProperty]
        private string _selectedFolderPath = string.Empty;

        [ObservableProperty]
        private bool _isScanning;

        [ObservableProperty]
        private string _statusMessage = "Ready. Select a folder to scan.";

        [ObservableProperty]
        private string _progressText = string.Empty;

        [ObservableProperty]
        private long _scannedFilesCount;

        [ObservableProperty]
        private long _scannedDirectoriesCount;

        [ObservableProperty]
        private long _totalScannedBytes;

        [ObservableProperty]
        private string _formattedTotalSize = "0 B";

        [ObservableProperty]
        private string _scanDurationText = string.Empty;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private FileSystemItemViewModel? _rootItem;

        [ObservableProperty]
        private FileSystemItemViewModel? _currentFolder;

        [ObservableProperty]
        private FileSystemItemViewModel? _selectedItem;

        [ObservableProperty]
        private ContentViewMode _viewMode = ContentViewMode.Split;

        public bool IsDetailsVisible => ViewMode == ContentViewMode.DetailsGrid || ViewMode == ContentViewMode.Split;
        public bool IsTreemapVisible => ViewMode == ContentViewMode.VisualTreemap || ViewMode == ContentViewMode.Split;
        public bool IsSplitView => ViewMode == ContentViewMode.Split;

        public ObservableCollection<BreadcrumbItemViewModel> Breadcrumbs { get; } = new();
        public ObservableCollection<FileSystemItemViewModel> FilteredItems { get; } = new();
        public ObservableCollection<FileSystemItemViewModel> RootNodes { get; } = new();

        public bool CanGoBack => _backHistory.Count > 0;
        public bool CanGoForward => _forwardHistory.Count > 0;
        public bool CanGoUp => CurrentFolder?.Parent != null;

        public MainViewModel(IDiskScannerService scannerService)
        {
            _scannerService = scannerService ?? throw new ArgumentNullException(nameof(scannerService));
        }

        public MainViewModel() : this(new DiskScannerService())
        {
        }

        partial void OnRootItemChanged(FileSystemItemViewModel? value)
        {
            RootNodes.Clear();
            if (value != null)
            {
                RootNodes.Add(value);
            }
        }

        partial void OnViewModeChanged(ContentViewMode value)
        {
            OnPropertyChanged(nameof(IsDetailsVisible));
            OnPropertyChanged(nameof(IsTreemapVisible));
            OnPropertyChanged(nameof(IsSplitView));
        }

        [RelayCommand]
        private void SelectViewMode(string modeStr)
        {
            if (Enum.TryParse<ContentViewMode>(modeStr, true, out var mode))
            {
                ViewMode = mode;
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilter();
        }

        partial void OnCurrentFolderChanged(FileSystemItemViewModel? value)
        {
            if (value != null)
            {
                var ancestor = value.Parent;
                while (ancestor != null)
                {
                    ancestor.IsExpanded = true;
                    ancestor = ancestor.Parent;
                }

                value.IsSelected = true;
            }

            UpdateBreadcrumbs(value);
            ApplyFilter();
            NotifyNavigationState();
        }

        private void NotifyNavigationState()
        {
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoForward));
            OnPropertyChanged(nameof(CanGoUp));
            NavigateBackCommand.NotifyCanExecuteChanged();
            NavigateForwardCommand.NotifyCanExecuteChanged();
            NavigateUpCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void BrowseFolder()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Folder to Scan"
            };

            if (!string.IsNullOrWhiteSpace(SelectedFolderPath) && Directory.Exists(SelectedFolderPath))
            {
                dialog.InitialDirectory = SelectedFolderPath;
            }

            if (dialog.ShowDialog() == true)
            {
                SelectedFolderPath = dialog.FolderName;
                StartScanCommand.Execute(null);
            }
        }

        [RelayCommand(CanExecute = nameof(CanStartScan))]
        private async Task StartScanAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedFolderPath) || !Directory.Exists(SelectedFolderPath))
            {
                StatusMessage = "Please select a valid folder path.";
                return;
            }

            _scanCts?.Cancel();
            _scanCts = new CancellationTokenSource();
            var token = _scanCts.Token;

            IsScanning = true;
            StatusMessage = $"Scanning {SelectedFolderPath}...";
            ProgressText = "Preparing...";
            ScannedFilesCount = 0;
            ScannedDirectoriesCount = 0;
            TotalScannedBytes = 0;
            FormattedTotalSize = "0 B";
            _backHistory.Clear();
            _forwardHistory.Clear();

            var stopwatch = Stopwatch.StartNew();

            var progress = new Progress<ScanProgressReport>(report =>
            {
                ScannedFilesCount = report.ScannedFiles;
                ScannedDirectoriesCount = report.ScannedDirectories;
                TotalScannedBytes = report.TotalBytes;
                FormattedTotalSize = ByteSizeFormatter.Format(report.TotalBytes);
                ProgressText = $"{report.ScannedFiles:N0} files, {report.ScannedDirectories:N0} folders ({FormattedTotalSize}) - {report.CurrentPath}";
            });

            try
            {
                var resultModel = await _scannerService.ScanDirectoryAsync(SelectedFolderPath, progress, token);
                stopwatch.Stop();

                var rootVm = new FileSystemItemViewModel(resultModel);
                RootItem = rootVm;
                CurrentFolder = rootVm;
                RootItem.IsExpanded = true;

                ScanDurationText = $"{stopwatch.Elapsed.TotalSeconds:F2}s";
                StatusMessage = $"Scan completed in {ScanDurationText}. Total: {ByteSizeFormatter.Format(RootItem.Size)}, {RootItem.FileCount:N0} files, {RootItem.DirectoryCount:N0} folders.";
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                StatusMessage = "Scan cancelled by user.";
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                StatusMessage = $"Scan failed: {ex.Message}";
            }
            finally
            {
                IsScanning = false;
                StartScanCommand.NotifyCanExecuteChanged();
                CancelScanCommand.NotifyCanExecuteChanged();
            }
        }

        private bool CanStartScan() => !IsScanning;

        [RelayCommand(CanExecute = nameof(CanCancelScan))]
        private void CancelScan()
        {
            _scanCts?.Cancel();
            StatusMessage = "Cancelling scan...";
        }

        private bool CanCancelScan() => IsScanning;

        [RelayCommand]
        public void NavigateToFolder(FileSystemItemViewModel? item)
        {
            if (item == null || !item.IsDirectory)
            {
                return;
            }

            if (CurrentFolder != null && CurrentFolder != item)
            {
                _backHistory.Push(CurrentFolder);
                _forwardHistory.Clear();
            }

            CurrentFolder = item;
            item.IsExpanded = true;
        }

        [RelayCommand(CanExecute = nameof(CanGoBack))]
        private void NavigateBack()
        {
            if (_backHistory.Count > 0 && CurrentFolder != null)
            {
                _forwardHistory.Push(CurrentFolder);
                var previous = _backHistory.Pop();
                CurrentFolder = previous;
            }
        }

        [RelayCommand(CanExecute = nameof(CanGoForward))]
        private void NavigateForward()
        {
            if (_forwardHistory.Count > 0 && CurrentFolder != null)
            {
                _backHistory.Push(CurrentFolder);
                var next = _forwardHistory.Pop();
                CurrentFolder = next;
            }
        }

        [RelayCommand(CanExecute = nameof(CanGoUp))]
        private void NavigateUp()
        {
            if (CurrentFolder?.Parent != null)
            {
                NavigateToFolder(CurrentFolder.Parent);
            }
        }

        [RelayCommand]
        private void NavigateBreadcrumb(BreadcrumbItemViewModel? breadcrumb)
        {
            if (breadcrumb?.Node != null)
            {
                NavigateToFolder(breadcrumb.Node);
            }
        }

        [RelayCommand]
        private void OpenInExplorer(FileSystemItemViewModel? item)
        {
            var targetItem = item ?? SelectedItem ?? CurrentFolder;
            if (targetItem == null || string.IsNullOrWhiteSpace(targetItem.FullPath))
            {
                return;
            }

            try
            {
                if (targetItem.IsDirectory && Directory.Exists(targetItem.FullPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = targetItem.FullPath,
                        UseShellExecute = true
                    });
                }
                else if (File.Exists(targetItem.FullPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{targetItem.FullPath}\"",
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Could not open Explorer: {ex.Message}";
            }
        }

        [RelayCommand]
        private void CopyPath(FileSystemItemViewModel? item)
        {
            var targetItem = item ?? SelectedItem ?? CurrentFolder;
            if (targetItem != null && !string.IsNullOrWhiteSpace(targetItem.FullPath))
            {
                Clipboard.SetText(targetItem.FullPath);
                StatusMessage = $"Copied path to clipboard: {targetItem.FullPath}";
            }
        }

        private void UpdateBreadcrumbs(FileSystemItemViewModel? folder)
        {
            Breadcrumbs.Clear();
            if (folder == null)
            {
                return;
            }

            var trail = new List<FileSystemItemViewModel>();
            var curr = folder;
            while (curr != null)
            {
                trail.Add(curr);
                curr = curr.Parent;
            }

            trail.Reverse();

            foreach (var node in trail)
            {
                Breadcrumbs.Add(new BreadcrumbItemViewModel(node.Name, node.FullPath, node));
            }
        }

        private void ApplyFilter()
        {
            FilteredItems.Clear();
            if (CurrentFolder == null)
            {
                return;
            }

            var query = SearchText?.Trim();
            var items = CurrentFolder.AllChildren.AsEnumerable();

            if (!string.IsNullOrEmpty(query))
            {
                items = items.Where(x =>
                    x.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    x.Extension.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var item in items)
            {
                FilteredItems.Add(item);
            }
        }
    }
}
