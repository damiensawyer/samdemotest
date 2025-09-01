using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Sam.Coach.Tests
{
    public class UnitTests
    {
        [Theory]
        [InlineData(new [] {4,3,5,8,5,0,0,-3}, new [] {3,5,8})]
        [InlineData(new [] {4,6,-3,3,7,9}, new [] {-3,3,7,9})]
        [InlineData(new [] {9,6,4,5,2,0}, new [] {4,5})]
        [InlineData(new [] {1,2,3,4,5}, new [] {1,2,3,4,5})]
        [InlineData(new [] {5,4,3,2,1}, new [] {5})]
        [InlineData(new int[] {}, new int[] {})]
        [InlineData(new [] {42}, new [] {42})]
        [InlineData(new [] {1,1,1,1}, new [] {1})]
        [InlineData(new [] {1,3,2,4,6,5,7,9,8}, new [] {2,4,6})]
        public async Task Can_Find(IEnumerable<int> data, IEnumerable<int> expected)
        {
            var finder = new LongestRisingSequenceFinder();
            var actual = await finder.Find(data);

            actual.Should().Equal(expected);
        }

        [Fact]
        public async Task Find_WithNullInput_ReturnsEmptyList()
        {
            var finder = new LongestRisingSequenceFinder();
            var actual = await finder.Find(null);

            actual.Should().NotBeNull();
            actual.Should().BeEmpty();
        }
    }
}
