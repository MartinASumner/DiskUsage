using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DiskUsage.Models;
using DiskUsage.Services;
using DiskUsage.ViewModels;
using Xunit;

namespace DiskUsage.Tests
{
    public class MainViewModelTests
    {
        private class MockScannerService : IDiskScannerService
        {
            private readonly FileSystemItem _cannedResult;

            public MockScannerService(FileSystemItem cannedResult)
            {
                _cannedResult = cannedResult;
            }

            public Task<FileSystemItem> ScanDirectoryAsync(
                string rootPath,
                IProgress<ScanProgressReport>? progress = null,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_cannedResult);
            }
        }

        [Fact]
        public async Task StartScanAsync_PopulatesRootAndCurrentFolderAndBreadcrumbs()
        {
            var rootModel = new FileSystemItem
            {
                Name = "Root",
                FullPath = @"C:\Root",
                ItemType = FileSystemItemType.Directory,
                Size = 1000,
                FileCount = 5,
                DirectoryCount = 2
            };

            var subDir = new FileSystemItem
            {
                Name = "SubDir",
                FullPath = @"C:\Root\SubDir",
                ItemType = FileSystemItemType.Directory,
                Size = 600,
                Parent = rootModel
            };
            rootModel.Children.Add(subDir);

            var scanner = new MockScannerService(rootModel);
            var vm = new MainViewModel(scanner)
            {
                SelectedFolderPath = Directory.GetCurrentDirectory() // valid path
            };

            await vm.StartScanCommand.ExecuteAsync(null);

            Assert.NotNull(vm.RootItem);
            Assert.NotNull(vm.CurrentFolder);
            Assert.Equal("Root", vm.CurrentFolder.Name);
            Assert.Single(vm.Breadcrumbs);
            Assert.Equal("Root", vm.Breadcrumbs[0].Name);
        }

        [Fact]
        public void Navigation_HistoryStack_BackForwardUpWorksCorrectly()
        {
            var rootModel = new FileSystemItem { Name = "Root", FullPath = @"C:\Root", ItemType = FileSystemItemType.Directory, Size = 1000 };
            var childModel = new FileSystemItem { Name = "Child", FullPath = @"C:\Root\Child", ItemType = FileSystemItemType.Directory, Size = 500, Parent = rootModel };
            var grandChildModel = new FileSystemItem { Name = "GrandChild", FullPath = @"C:\Root\Child\GrandChild", ItemType = FileSystemItemType.Directory, Size = 200, Parent = childModel };

            rootModel.Children.Add(childModel);
            childModel.Children.Add(grandChildModel);

            var rootVm = new FileSystemItemViewModel(rootModel);
            var childVm = rootVm.DirectoryChildren[0];
            var grandChildVm = childVm.DirectoryChildren[0];

            var vm = new MainViewModel(new MockScannerService(rootModel))
            {
                RootItem = rootVm,
                CurrentFolder = rootVm
            };

            Assert.False(vm.CanGoBack);
            Assert.False(vm.CanGoForward);
            Assert.False(vm.CanGoUp);

            // Drill down into Child
            vm.NavigateToFolder(childVm);
            Assert.Equal("Child", vm.CurrentFolder.Name);
            Assert.True(vm.CanGoBack);
            Assert.True(vm.CanGoUp);
            Assert.Equal(2, vm.Breadcrumbs.Count);

            // Drill down into GrandChild
            vm.NavigateToFolder(grandChildVm);
            Assert.Equal("GrandChild", vm.CurrentFolder.Name);
            Assert.Equal(3, vm.Breadcrumbs.Count);

            // Navigate Back -> Child
            vm.NavigateBackCommand.Execute(null);
            Assert.Equal("Child", vm.CurrentFolder.Name);
            Assert.True(vm.CanGoForward);

            // Navigate Forward -> GrandChild
            vm.NavigateForwardCommand.Execute(null);
            Assert.Equal("GrandChild", vm.CurrentFolder.Name);

            // Navigate Up -> Child
            vm.NavigateUpCommand.Execute(null);
            Assert.Equal("Child", vm.CurrentFolder.Name);

            // Navigate Up -> Root
            vm.NavigateUpCommand.Execute(null);
            Assert.Equal("Root", vm.CurrentFolder.Name);
            Assert.False(vm.CanGoUp);
        }

        [Fact]
        public void SearchFilter_FiltersItemsByQuery()
        {
            var rootModel = new FileSystemItem { Name = "Root", ItemType = FileSystemItemType.Directory };
            rootModel.Children.Add(new FileSystemItem { Name = "ImportantReport.pdf", Extension = ".pdf", ItemType = FileSystemItemType.File });
            rootModel.Children.Add(new FileSystemItem { Name = "Budget.xlsx", Extension = ".xlsx", ItemType = FileSystemItemType.File });
            rootModel.Children.Add(new FileSystemItem { Name = "Notes.txt", Extension = ".txt", ItemType = FileSystemItemType.File });

            var rootVm = new FileSystemItemViewModel(rootModel);
            var vm = new MainViewModel(new MockScannerService(rootModel))
            {
                RootItem = rootVm,
                CurrentFolder = rootVm
            };

            Assert.Equal(3, vm.FilteredItems.Count);

            // Filter by "pdf"
            vm.SearchText = "pdf";
            Assert.Single(vm.FilteredItems);
            Assert.Equal("ImportantReport.pdf", vm.FilteredItems[0].Name);

            // Filter by "Budget"
            vm.SearchText = "budget";
            Assert.Single(vm.FilteredItems);
            Assert.Equal("Budget.xlsx", vm.FilteredItems[0].Name);

            // Clear filter
            vm.SearchText = string.Empty;
            Assert.Equal(3, vm.FilteredItems.Count);
        }

        [Fact]
        public void ViewMode_Switching_UpdatesVisibilityProperties()
        {
            var vm = new MainViewModel();

            // Default is Split
            Assert.Equal(ContentViewMode.Split, vm.ViewMode);
            Assert.True(vm.IsDetailsVisible);
            Assert.True(vm.IsTreemapVisible);
            Assert.True(vm.IsSplitView);

            // Switch to DetailsGrid
            vm.SelectViewModeCommand.Execute("DetailsGrid");
            Assert.Equal(ContentViewMode.DetailsGrid, vm.ViewMode);
            Assert.True(vm.IsDetailsVisible);
            Assert.False(vm.IsTreemapVisible);
            Assert.False(vm.IsSplitView);

            // Switch to VisualTreemap
            vm.SelectViewModeCommand.Execute("VisualTreemap");
            Assert.Equal(ContentViewMode.VisualTreemap, vm.ViewMode);
            Assert.False(vm.IsDetailsVisible);
            Assert.True(vm.IsTreemapVisible);
            Assert.False(vm.IsSplitView);

            // Switch back to Split
            vm.SelectViewModeCommand.Execute("Split");
            Assert.Equal(ContentViewMode.Split, vm.ViewMode);
            Assert.True(vm.IsDetailsVisible);
            Assert.True(vm.IsTreemapVisible);
            Assert.True(vm.IsSplitView);
        }
    }
}
