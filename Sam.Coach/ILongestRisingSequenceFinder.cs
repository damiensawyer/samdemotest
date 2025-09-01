using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sam.Coach
{
    public interface ILongestRisingSequenceFinder {
        Task<IEnumerable<int>> Find(IEnumerable<int> numbers);
    }
}