using DiskUsage.Helpers;
using DiskUsage.Models;
using Xunit;

namespace DiskUsage.Tests
{
    public class FileCategoryHelperTests
    {
        [Theory]
        [InlineData("video.mp4", FileCategory.Media)]
        [InlineData("song.mp3", FileCategory.Media)]
        [InlineData("archive.zip", FileCategory.Archive)]
        [InlineData("package.tar.gz", FileCategory.Archive)]
        [InlineData("program.cs", FileCategory.Code)]
        [InlineData("script.py", FileCategory.Code)]
        [InlineData("photo.png", FileCategory.Image)]
        [InlineData("diagram.svg", FileCategory.Image)]
        [InlineData("app.exe", FileCategory.Executable)]
        [InlineData("library.dll", FileCategory.Executable)]
        [InlineData("doc.pdf", FileCategory.Document)]
        [InlineData("sheet.xlsx", FileCategory.Document)]
        [InlineData("custom.xyz123", FileCategory.Other)]
        public void Categorize_IdentifiesCorrectCategoryByExtension(string fileName, FileCategory expected)
        {
            var item = new FileSystemItem
            {
                Name = fileName,
                Extension = System.IO.Path.GetExtension(fileName),
                ItemType = FileSystemItemType.File
            };

            var category = FileCategoryHelper.Categorize(item);
            Assert.Equal(expected, category);
        }

        [Fact]
        public void Categorize_Directory_ReturnsDirectoryCategory()
        {
            var item = new FileSystemItem
            {
                Name = "MyFolder",
                ItemType = FileSystemItemType.Directory
            };

            var category = FileCategoryHelper.Categorize(item);
            Assert.Equal(FileCategory.Directory, category);
        }

        [Fact]
        public void GetCategoryColor_ReturnsValidNonTransparentColors()
        {
            foreach (FileCategory cat in System.Enum.GetValues(typeof(FileCategory)))
            {
                var color = FileCategoryHelper.GetCategoryColor(cat);
                Assert.Equal(255, color.A); // Solid opacity
            }
        }
    }
}
