using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleUI.Problems.Collections
{
    /*
       1 Stack implementation
       2 Queue implementation
       3 Dictionary lookup
       4 HashSet duplicates removal
       5 LinkedList traversal
       6 Implement custom stack
       7 Implement custom queue
       8 Detect duplicate numbers
       9 Frequency counter
       10 Merge lists
       11 Find intersection of lists
       12 Find union
       13 Find difference
       14 Convert list to dictionary
       15 Flatten nested collection
       16 Sort dictionary by value
       17 Get top frequent elements
       18 Reverse list
       19 Find median
       20 Find mode
     */

    public class CollectionProblems
    {
        /* 1 */
        public void StackExample()
        {
            Console.WriteLine("1 Stack Example");

            Stack<int> stack = new Stack<int>();

            stack.Push(10);
            stack.Push(20);
            stack.Push(30);

            Console.WriteLine(stack.Pop());
        }

        /* 2 */
        public void QueueExample()
        {
            Console.WriteLine("2 Queue Example");

            Queue<int> queue = new Queue<int>();

            queue.Enqueue(10);
            queue.Enqueue(20);
            queue.Enqueue(30);

            Console.WriteLine(queue.Dequeue());
        }

        /* 3 */
        public void DictionaryLookup()
        {
            Console.WriteLine("3 Dictionary Lookup");

            Dictionary<int, string> dict = new Dictionary<int, string>()
            {
                {1,"Alice"},
                {2,"Bob"}
            };

            Console.WriteLine(dict[1]);
        }

        /* 4 */
        public void HashSetRemoveDuplicates()
        {
            Console.WriteLine("4 HashSet Remove Duplicates");

            int[] nums = { 1, 2, 2, 3, 4, 4 };

            HashSet<int> set = new HashSet<int>(nums);

            Console.WriteLine(string.Join(",", set));
        }

        /* 5 */
        public void LinkedListTraversal()
        {
            Console.WriteLine("5 LinkedList Traversal");

            LinkedList<int> list = new LinkedList<int>();

            list.AddLast(10);
            list.AddLast(20);
            list.AddLast(30);

            foreach (var item in list)
                Console.WriteLine(item);
        }

        /* 6 */
        public void CustomStack()
        {
            Console.WriteLine("6 Custom Stack");

            List<int> stack = new List<int>();

            stack.Add(10);
            stack.Add(20);

            int top = stack.Last();
            stack.RemoveAt(stack.Count - 1);

            Console.WriteLine(top);
        }

        /* 7 */
        public void CustomQueue()
        {
            Console.WriteLine("7 Custom Queue");

            List<int> queue = new List<int>();

            queue.Add(10);
            queue.Add(20);

            int first = queue[0];
            queue.RemoveAt(0);

            Console.WriteLine(first);
        }

        /* 8 */
        public void DetectDuplicates()
        {
            Console.WriteLine("8 Detect Duplicate Numbers");

            int[] nums = { 1, 2, 3, 3, 4, 5, 5 };

            var duplicates = nums
                .GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            Console.WriteLine(string.Join(",", duplicates));
        }

        /* 9 */
        public void FrequencyCounter()
        {
            Console.WriteLine("9 Frequency Counter");

            string word = "programming";

            var result = word
                .GroupBy(c => c)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var item in result)
                Console.WriteLine($"{item.Key}:{item.Value}");
        }

        /* 10 */
        public void MergeLists()
        {
            Console.WriteLine("10 Merge Lists");

            List<int> a = new List<int> { 1, 2, 3 };
            List<int> b = new List<int> { 4, 5, 6 };

            var merged = a.Concat(b);

            Console.WriteLine(string.Join(",", merged));
        }

        /* 11 */
        public void FindIntersection()
        {
            Console.WriteLine("11 Intersection");

            var a = new List<int> { 1, 2, 3 };
            var b = new List<int> { 2, 3, 4 };

            var result = a.Intersect(b);

            Console.WriteLine(string.Join(",", result));
        }

        /* 12 */
        public void FindUnion()
        {
            Console.WriteLine("12 Union");

            var a = new List<int> { 1, 2 };
            var b = new List<int> { 2, 3 };

            var result = a.Union(b);

            Console.WriteLine(string.Join(",", result));
        }

        /* 13 */
        public void FindDifference()
        {
            Console.WriteLine("13 Difference");

            var a = new List<int> { 1, 2, 3 };
            var b = new List<int> { 2 };

            var result = a.Except(b);

            Console.WriteLine(string.Join(",", result));
        }

        /* 14 */
        public void ListToDictionary()
        {
            Console.WriteLine("14 Convert List To Dictionary");

            var list = new List<string> { "A", "B", "C" };

            var dict = list.Select((value, index) => new { index, value })
                .ToDictionary(x => x.index, x => x.value);

            foreach (var item in dict)
                Console.WriteLine($"{item.Key}:{item.Value}");
        }

        /* 15 */
        public void FlattenNestedCollection()
        {
            Console.WriteLine("15 Flatten Nested Collection");

            var nested = new List<List<int>>
            {
                new(){1,2},
                new(){3,4}
            };

            var flat = nested.SelectMany(x => x);

            Console.WriteLine(string.Join(",", flat));
        }

        /* 16 */
        public void SortDictionaryByValue()
        {
            Console.WriteLine("16 Sort Dictionary By Value");

            Dictionary<string, int> dict = new()
            {
                {"A",3},
                {"B",1},
                {"C",2}
            };

            var sorted = dict.OrderBy(x => x.Value);

            foreach (var item in sorted)
                Console.WriteLine($"{item.Key}:{item.Value}");
        }

        /* 17 */
        public void TopFrequentElements()
        {
            Console.WriteLine("17 Top Frequent Elements");

            int[] nums = { 1, 1, 1, 2, 2, 3 };

            var result = nums
                .GroupBy(x => x)
                .OrderByDescending(g => g.Count())
                .Take(2)
                .Select(g => g.Key);

            Console.WriteLine(string.Join(",", result));
        }

        /* 18 */
        public void ReverseList()
        {
            Console.WriteLine("18 Reverse List");

            var list = new List<int> { 1, 2, 3, 4 };

            list.Reverse();

            Console.WriteLine(string.Join(",", list));
        }

        /* 19 */
        public void FindMedian()
        {
            Console.WriteLine("19 Find Median");

            var nums = new List<int> { 1, 3, 5, 7, 9 };

            nums.Sort();

            int mid = nums.Count / 2;

            Console.WriteLine(nums[mid]);
        }

        /* 20 */
        public void FindMode()
        {
            Console.WriteLine("20 Find Mode");

            int[] nums = { 1, 2, 2, 3, 3, 3, 4 };

            var mode = nums
                .GroupBy(x => x)
                .OrderByDescending(g => g.Count())
                .First()
                .Key;

            Console.WriteLine(mode);
        }
    }

}
