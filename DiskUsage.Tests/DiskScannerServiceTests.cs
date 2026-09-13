using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DiskUsage.Models;
using DiskUsage.Services;
using Xunit;

namespace DiskUsage.Tests
{
    public class DiskScannerServiceTests : IDisposable
    {
        private readonly string _testRoot;

        public DiskScannerServiceTests()
        {
            _testRoot = Path.Combine(Path.GetTempPath(), "DiskUsageTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testRoot);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testRoot))
                {
                    Directory.Delete(_testRoot, true);
                }
            }
            catch
            {
                // Best effort cleanup
            }
        }

        [Fact]
        public async Task ScanDirectoryAsync_EmptyFolder_ReturnsZeroSizeAndCorrectCounts()
        {
            var service = new DiskScannerService();
            var result = await service.ScanDirectoryAsync(_testRoot);

            Assert.NotNull(result);
            Assert.Equal(0, result.Size);
            Assert.Equal(0, result.FileCount);
            Assert.Equal(0, result.DirectoryCount);
            Assert.Empty(result.Children);
        }

        [Fact]
        public async Task ScanDirectoryAsync_CalculatesRecursiveSizesAndCounts()
        {
            // Structure:
            // _testRoot/
            //   file1.txt (100 bytes)
            //   file2.txt (200 bytes)
            //   Sub1/
            //     subfile1.bin (300 bytes)
            //     Sub2/
            //       subsubfile.dat (400 bytes)

            File.WriteAllBytes(Path.Combine(_testRoot, "file1.txt"), new byte[100]);
            File.WriteAllBytes(Path.Combine(_testRoot, "file2.txt"), new byte[200]);

            var sub1 = Path.Combine(_testRoot, "Sub1");
            Directory.CreateDirectory(sub1);
            File.WriteAllBytes(Path.Combine(sub1, "subfile1.bin"), new byte[300]);

            var sub2 = Path.Combine(sub1, "Sub2");
            Directory.CreateDirectory(sub2);
            File.WriteAllBytes(Path.Combine(sub2, "subsubfile.dat"), new byte[400]);

            var service = new DiskScannerService();
            var result = await service.ScanDirectoryAsync(_testRoot);

            Assert.Equal(1000, result.Size);
            Assert.Equal(4, result.FileCount);
            Assert.Equal(2, result.DirectoryCount);
            Assert.Equal(3, result.Children.Count); // file1.txt, file2.txt, Sub1

            // Largest child should be Sub1 (700 bytes)
            var sub1Item = result.Children.Find(c => c.Name == "Sub1");
            Assert.NotNull(sub1Item);
            Assert.Equal(700, sub1Item.Size);
            Assert.Equal(2, sub1Item.FileCount);
            Assert.Equal(1, sub1Item.DirectoryCount);

            // Sub2
            var sub2Item = sub1Item.Children.Find(c => c.Name == "Sub2");
            Assert.NotNull(sub2Item);
            Assert.Equal(400, sub2Item.Size);
            Assert.Equal(1, sub2Item.FileCount);
            Assert.Equal(0, sub2Item.DirectoryCount);
        }

        [Fact]
        public async Task ScanDirectoryAsync_NonExistentPath_ThrowsDirectoryNotFoundException()
        {
            var service = new DiskScannerService();
            var nonExistent = Path.Combine(_testRoot, "NonExistentFolder_12345");

            await Assert.ThrowsAsync<DirectoryNotFoundException>(() => service.ScanDirectoryAsync(nonExistent));
        }

        [Fact]
        public async Task ScanDirectoryAsync_Cancellation_ThrowsOperationCanceledException()
        {
            var sub = Path.Combine(_testRoot, "Sub");
            Directory.CreateDirectory(sub);
            for (int i = 0; i < 50; i++)
            {
                File.WriteAllBytes(Path.Combine(sub, $"file_{i}.dat"), new byte[10]);
            }

            var service = new DiskScannerService();
            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Pre-cancel

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.ScanDirectoryAsync(_testRoot, cancellationToken: cts.Token));
        }
    }
}
