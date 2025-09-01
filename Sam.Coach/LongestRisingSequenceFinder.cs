using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sam.Coach
{
    public class LongestRisingSequenceFinder : ILongestRisingSequenceFinder
    {
        public Task<IEnumerable<int>> Find(IEnumerable<int> numbers) => Task.Run(() =>
        {
            if (numbers == null)
                return new List<int>();

            var numbersList = numbers.ToList();
            if (numbersList.Count <= 1)
                return numbersList;

            var longestSequence = new List<int>();
            var currentSequence = new List<int> { numbersList[0] };

            for (int i = 1; i < numbersList.Count; i++)
            {
                if (numbersList[i] > numbersList[i - 1])
                {
                    currentSequence.Add(numbersList[i]);
                }
                else
                {
                    if (currentSequence.Count > longestSequence.Count)
                    {
                        longestSequence = new List<int>(currentSequence);
                    }
                    currentSequence = new List<int> { numbersList[i] };
                }
            }

            if (currentSequence.Count > longestSequence.Count)
            {
                longestSequence = currentSequence;
            }

            return longestSequence.AsEnumerable();
        });
    }
}
