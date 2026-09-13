using DiskUsage.Helpers;
using Xunit;

namespace DiskUsage.Tests
{
    public class ByteSizeFormatterTests
    {
        [Theory]
        [InlineData(0, "0 B")]
        [InlineData(-50, "0 B")]
        [InlineData(500, "500 B")]
        [InlineData(1023, "1023 B")]
        [InlineData(1024, "1 KB")]
        [InlineData(1536, "1.5 KB")]
        [InlineData(1048576, "1 MB")]
        [InlineData(1572864, "1.5 MB")]
        [InlineData(1073741824, "1 GB")]
        [InlineData(5368709120, "5 GB")]
        [InlineData(1099511627776, "1 TB")]
        public void Format_ReturnsExpectedString(long bytes, string expected)
        {
            var result = ByteSizeFormatter.Format(bytes);
            Assert.Equal(expected, result);
        }
    }
}
