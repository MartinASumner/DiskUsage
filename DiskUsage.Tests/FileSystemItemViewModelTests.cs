using System;
using DiskUsage.Models;
using DiskUsage.ViewModels;
using Xunit;

namespace DiskUsage.Tests
{
    public class FileSystemItemViewModelTests
    {
        [Fact]
        public void PercentageOfParent_CalculatesAccurately()
        {
            var parentModel = new FileSystemItem
            {
                Name = "Parent",
                FullPath = @"C:\Parent",
                ItemType = FileSystemItemType.Directory,
                Size = 1000
            };

            var childModel1 = new FileSystemItem
            {
                Name = "Child1",
                FullPath = @"C:\Parent\Child1",
                ItemType = FileSystemItemType.Directory,
                Size = 500
            };

            var childModel2 = new FileSystemItem
            {
                Name = "Child2.txt",
                FullPath = @"C:\Parent\Child2.txt",
                ItemType = FileSystemItemType.File,
                Size = 250
            };

            parentModel.Children.Add(childModel1);
            parentModel.Children.Add(childModel2);

            var parentVm = new FileSystemItemViewModel(parentModel);
            Assert.Equal(100.0, parentVm.PercentOfParent);

            var child1Vm = parentVm.AllChildren[0];
            Assert.Equal(50.0, child1Vm.PercentOfParent);
            Assert.Equal("50.0%", child1Vm.PercentOfParentText);

            var child2Vm = parentVm.AllChildren[1];
            Assert.Equal(25.0, child2Vm.PercentOfParent);
            Assert.Equal("25.0%", child2Vm.PercentOfParentText);
        }

        [Fact]
        public void DirectoryChildren_OnlyContainsDirectories()
        {
            var parentModel = new FileSystemItem
            {
                Name = "Parent",
                ItemType = FileSystemItemType.Directory,
                Size = 300
            };

            parentModel.Children.Add(new FileSystemItem { Name = "Dir1", ItemType = FileSystemItemType.Directory, Size = 100 });
            parentModel.Children.Add(new FileSystemItem { Name = "File1.txt", ItemType = FileSystemItemType.File, Size = 200 });

            var parentVm = new FileSystemItemViewModel(parentModel);

            Assert.Single(parentVm.DirectoryChildren);
            Assert.Equal("Dir1", parentVm.DirectoryChildren[0].Name);
            Assert.Equal(2, parentVm.AllChildren.Count);
        }

        [Fact]
        public void DirectoryChildren_SharesSameInstancesWithAllChildren()
        {
            var parentModel = new FileSystemItem
            {
                Name = "Parent",
                ItemType = FileSystemItemType.Directory,
                Size = 300
            };

            parentModel.Children.Add(new FileSystemItem { Name = "Dir1", ItemType = FileSystemItemType.Directory, Size = 100 });
            parentModel.Children.Add(new FileSystemItem { Name = "File1.txt", ItemType = FileSystemItemType.File, Size = 200 });

            var parentVm = new FileSystemItemViewModel(parentModel);

            Assert.Same(parentVm.AllChildren[0], parentVm.DirectoryChildren[0]);
        }
    }
}
