using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using DiskUsage.Models;
using DiskUsage.Services;
using DiskUsage.ViewModels;
using Xunit;

namespace DiskUsage.Tests
{
    public class TreemapLayoutEngineTests
    {
        private readonly TreemapLayoutEngine _engine = new();

        [Fact]
        public void ComputeLayout_NullOrEmptyItems_ReturnsEmpty()
        {
            var bounds = new Rect(0, 0, 800, 600);
            var result = _engine.ComputeLayout(Array.Empty<FileSystemItemViewModel>(), bounds);
            Assert.Empty(result);
        }

        [Fact]
        public void ComputeLayout_InvalidBounds_ReturnsEmpty()
        {
            var item = new FileSystemItemViewModel(new FileSystemItem { Name = "test.txt", Size = 100, ItemType = FileSystemItemType.File });
            var result = _engine.ComputeLayout(new[] { item }, new Rect(0, 0, 0, 0));
            Assert.Empty(result);
        }

        [Fact]
        public void ComputeLayout_SingleItem_FillsEntireContainerBounds()
        {
            var item = new FileSystemItemViewModel(new FileSystemItem { Name = "large.iso", Size = 5000, ItemType = FileSystemItemType.File });
            var bounds = new Rect(0, 0, 500, 300);

            var result = _engine.ComputeLayout(new[] { item }, bounds);

            Assert.Single(result);
            var node = result[0];
            Assert.Equal(0, node.Bounds.X);
            Assert.Equal(0, node.Bounds.Y);
            Assert.Equal(500, node.Bounds.Width, 1);
            Assert.Equal(300, node.Bounds.Height, 1);
        }

        [Fact]
        public void ComputeLayout_MultipleItems_TotalAreaMatchesContainerArea()
        {
            var items = new List<FileSystemItemViewModel>
            {
                new(new FileSystemItem { Name = "file1.mp4", Size = 600, ItemType = FileSystemItemType.File }),
                new(new FileSystemItem { Name = "file2.zip", Size = 300, ItemType = FileSystemItemType.File }),
                new(new FileSystemItem { Name = "file3.cs", Size = 100, ItemType = FileSystemItemType.File })
            };

            var bounds = new Rect(0, 0, 1000, 800);
            double expectedTotalArea = bounds.Width * bounds.Height;

            var result = _engine.ComputeLayout(items, bounds);

            Assert.Equal(3, result.Count);

            double actualTotalArea = result.Sum(n => n.Bounds.Width * n.Bounds.Height);
            Assert.Equal(expectedTotalArea, actualTotalArea, 0.1);

            // Proportional verification (file1 should be ~60% of total area)
            var node1 = result.First(n => n.Name == "file1.mp4");
            double node1Area = node1.Bounds.Width * node1.Bounds.Height;
            Assert.Equal(0.60 * expectedTotalArea, node1Area, 1.0);
        }

        [Fact]
        public void ComputeLayout_ZeroSizeItems_AreFilteredOut()
        {
            var items = new List<FileSystemItemViewModel>
            {
                new(new FileSystemItem { Name = "file1.txt", Size = 500, ItemType = FileSystemItemType.File }),
                new(new FileSystemItem { Name = "empty.txt", Size = 0, ItemType = FileSystemItemType.File })
            };

            var bounds = new Rect(0, 0, 400, 400);
            var result = _engine.ComputeLayout(items, bounds);

            Assert.Single(result);
            Assert.Equal("file1.txt", result[0].Name);
        }

        [Fact]
        public void ComputeLayout_AllNodesContainedWithinBounds()
        {
            var items = new List<FileSystemItemViewModel>
            {
                new(new FileSystemItem { Name = "f1", Size = 1000, ItemType = FileSystemItemType.File }),
                new(new FileSystemItem { Name = "f2", Size = 800, ItemType = FileSystemItemType.File }),
                new(new FileSystemItem { Name = "f3", Size = 400, ItemType = FileSystemItemType.File }),
                new(new FileSystemItem { Name = "f4", Size = 200, ItemType = FileSystemItemType.File }),
                new(new FileSystemItem { Name = "f5", Size = 100, ItemType = FileSystemItemType.File })
            };

            var bounds = new Rect(10, 10, 800, 600);
            var result = _engine.ComputeLayout(items, bounds);

            Assert.Equal(5, result.Count);

            foreach (var node in result)
            {
                Assert.True(node.Bounds.Left >= bounds.Left - 0.01);
                Assert.True(node.Bounds.Top >= bounds.Top - 0.01);
                Assert.True(node.Bounds.Right <= bounds.Right + 0.01);
                Assert.True(node.Bounds.Bottom <= bounds.Bottom + 0.01);
            }
        }
    }
}
