using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using DiskUsage.Models;
using DiskUsage.ViewModels;

namespace DiskUsage.Services
{
    public class TreemapLayoutEngine : ITreemapLayoutEngine
    {
        private class LayoutItem
        {
            public FileSystemItemViewModel Item { get; }
            public double NormalizedArea { get; set; }

            public LayoutItem(FileSystemItemViewModel item, double normalizedArea)
            {
                Item = item;
                NormalizedArea = normalizedArea;
            }
        }

        public IReadOnlyList<TreemapNode> ComputeLayout(IReadOnlyList<FileSystemItemViewModel> items, Rect containerBounds)
        {
            if (items == null || items.Count == 0 || containerBounds.Width <= 1 || containerBounds.Height <= 1)
            {
                return Array.Empty<TreemapNode>();
            }

            // Only layout items with size > 0
            var positiveItems = items.Where(i => i.Size > 0).ToList();
            if (positiveItems.Count == 0)
            {
                return Array.Empty<TreemapNode>();
            }

            double totalSize = positiveItems.Sum(i => (double)i.Size);
            if (totalSize <= 0)
            {
                return Array.Empty<TreemapNode>();
            }

            double totalArea = containerBounds.Width * containerBounds.Height;
            double areaScale = totalArea / totalSize;

            var layoutItems = positiveItems
                .OrderByDescending(i => i.Size)
                .Select(i => new LayoutItem(i, (double)i.Size * areaScale))
                .ToList();

            var result = new List<TreemapNode>();
            var remainingBounds = containerBounds;
            var currentRow = new List<LayoutItem>();

            foreach (var item in layoutItems)
            {
                if (currentRow.Count == 0)
                {
                    currentRow.Add(item);
                }
                else
                {
                    double side = Math.Min(remainingBounds.Width, remainingBounds.Height);
                    if (side <= 0) break;

                    double currentWorst = WorstAspectRatio(currentRow, side);
                    var candidateRow = new List<LayoutItem>(currentRow) { item };
                    double candidateWorst = WorstAspectRatio(candidateRow, side);

                    if (candidateWorst <= currentWorst)
                    {
                        currentRow.Add(item);
                    }
                    else
                    {
                        LayoutRow(currentRow, ref remainingBounds, result);
                        currentRow.Clear();
                        currentRow.Add(item);
                    }
                }
            }

            if (currentRow.Count > 0)
            {
                LayoutRow(currentRow, ref remainingBounds, result);
            }

            return result;
        }

        private static double WorstAspectRatio(List<LayoutItem> row, double sideLength)
        {
            if (row.Count == 0 || sideLength <= 0) return double.MaxValue;

            double sumArea = row.Sum(x => x.NormalizedArea);
            if (sumArea <= 0) return double.MaxValue;

            double maxArea = row.Max(x => x.NormalizedArea);
            double minArea = row.Min(x => x.NormalizedArea);

            double sideSq = sideLength * sideLength;
            double sumSq = sumArea * sumArea;

            return Math.Max((sideSq * maxArea) / sumSq, sumSq / (sideSq * minArea));
        }

        private static void LayoutRow(List<LayoutItem> row, ref Rect remainingBounds, List<TreemapNode> result)
        {
            if (row.Count == 0 || remainingBounds.Width <= 0 || remainingBounds.Height <= 0) return;

            double sumArea = row.Sum(x => x.NormalizedArea);
            if (sumArea <= 0) return;

            bool isHorizontal = remainingBounds.Width <= remainingBounds.Height;
            double sideLength = isHorizontal ? remainingBounds.Width : remainingBounds.Height;

            if (sideLength <= 0) return;

            double rowThickness = Math.Min(isHorizontal ? remainingBounds.Height : remainingBounds.Width, sumArea / sideLength);

            if (isHorizontal)
            {
                // Layout horizontally along top of remaining bounds
                double currentX = remainingBounds.X;
                for (int i = 0; i < row.Count; i++)
                {
                    var item = row[i];
                    double itemWidth = (i == row.Count - 1)
                        ? Math.Max(0, remainingBounds.Right - currentX)
                        : (item.NormalizedArea / rowThickness);

                    var rect = new Rect(currentX, remainingBounds.Y, Math.Max(0, itemWidth), Math.Max(0, rowThickness));
                    result.Add(new TreemapNode(item.Item, rect));
                    currentX += itemWidth;
                }

                double newY = remainingBounds.Y + rowThickness;
                double newHeight = Math.Max(0, remainingBounds.Height - rowThickness);
                remainingBounds = new Rect(remainingBounds.X, newY, remainingBounds.Width, newHeight);
            }
            else
            {
                // Layout vertically along left of remaining bounds
                double currentY = remainingBounds.Y;
                for (int i = 0; i < row.Count; i++)
                {
                    var item = row[i];
                    double itemHeight = (i == row.Count - 1)
                        ? Math.Max(0, remainingBounds.Bottom - currentY)
                        : (item.NormalizedArea / rowThickness);

                    var rect = new Rect(remainingBounds.X, currentY, Math.Max(0, rowThickness), Math.Max(0, itemHeight));
                    result.Add(new TreemapNode(item.Item, rect));
                    currentY += itemHeight;
                }

                double newX = remainingBounds.X + rowThickness;
                double newWidth = Math.Max(0, remainingBounds.Width - rowThickness);
                remainingBounds = new Rect(newX, remainingBounds.Y, newWidth, remainingBounds.Height);
            }
        }
    }
}
