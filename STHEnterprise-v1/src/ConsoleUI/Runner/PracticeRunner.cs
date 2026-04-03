using ConsoleUI.Problems.Basic;
using ConsoleUI.Problems.Collections;
using ConsoleUI.Problems.DesignPatterns;
using ConsoleUI.Problems.LINQ;
using ConsoleUI.Problems.Multithreading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static ConsoleUI.Problems.DesignPatterns.DesignPatternProblems;
using System.Windows.Input;
using ConsoleUI.Utilities;

namespace ConsoleUI.Runner
{
    public class PracticeRunner
    {
        public async Task RunAsync()
        {
            var basic = new BasicProblems();
            var linq = new LinqProblems();
            var thread = new ThreadingProblems();
            var collections = new CollectionProblems();
            var dp = new DesignPatternProblems();

            ConsoleHelper.PrintHeader("BASIC PROBLEMS");
           // Console.WriteLine("\n================ BASIC PROBLEMS =================");

            Console.WriteLine("1 Reverse String: " + basic.ReverseString("Interview"));
            Console.WriteLine("2 Palindrome: " + basic.IsPalindrome("madam"));
            Console.WriteLine("3 Duplicate Characters: " + string.Join(",", basic.GetDuplicateCharacters("programming")));

            Console.WriteLine("4 Character Frequency:");
            foreach (var f in basic.CharacterFrequency("hello"))
                Console.WriteLine($"{f.Key}:{f.Value}");

            Console.WriteLine("5 Second Largest: " + basic.SecondLargest(new[] { 10, 20, 30, 40 }));

            var swap = basic.SwapNumbers(10, 20);
            Console.WriteLine($"6 Swap Numbers: a={swap.Item1} b={swap.Item2}");

            Console.WriteLine("7 Fibonacci: " + string.Join(",", basic.Fibonacci(10)));
            Console.WriteLine("8 Prime: " + basic.IsPrime(29));
            Console.WriteLine("9 Factorial: " + basic.Factorial(5));
            Console.WriteLine("10 Armstrong: " + basic.IsArmstrong(153));
            Console.WriteLine("11 Missing Number: " + basic.FindMissingNumber(new[] { 1, 2, 3, 5 }, 5));
            Console.WriteLine("12 Sum Digits: " + basic.SumOfDigits(1234));
            Console.WriteLine("13 Reverse Number: " + basic.ReverseNumber(1234));

            var eo = basic.EvenOddFilter(new[] { 1, 2, 3, 4, 5, 6 });
            Console.WriteLine("14 Even: " + string.Join(",", eo.Item1));
            Console.WriteLine("   Odd: " + string.Join(",", eo.Item2));

            Console.WriteLine("15 Max: " + basic.MaxNumber(new[] { 2, 9, 4, 7 }));
            Console.WriteLine("16 Min: " + basic.MinNumber(new[] { 2, 9, 4, 7 }));
            Console.WriteLine("17 Remove Duplicates: " + string.Join(",", basic.RemoveDuplicates(new[] { 1, 2, 2, 3, 4 })));
            Console.WriteLine("18 Vowel Count: " + basic.CountVowels("Interview"));
            Console.WriteLine("19 Word Count: " + basic.CountWords("C sharp coding interview practice"));
            Console.WriteLine("20 Sort Numbers: " + string.Join(",", basic.SortNumbers(new[] { 5, 3, 1, 4, 2 })));

            ConsoleHelper.PrintHeader("LINQ PROBLEMS");
            //Console.WriteLine("\n================ LINQ PROBLEMS =================");

            linq.GroupByDepartment();
            linq.HighestSalary();
            linq.Top3Salaries();
            linq.RemoveDuplicates();
            linq.OrderBySalary();
            linq.ConvertToDictionary();
            linq.CountPerDepartment();
            linq.FindEmployeeByName();
            linq.AverageSalary();
            linq.AboveAverageSalary();
            linq.JoinCollections();
            linq.DistinctDepartments();
            linq.SelectProjection();
            linq.FlattenNestedList();
            linq.FindDuplicateNumbers();
            linq.PagingExample();
            linq.SortMultipleColumns();
            linq.FilterExample();
            linq.AggregateSum();
            linq.GroupByMultipleColumns();

            ConsoleHelper.PrintHeader("MULTITHREADING PROBLEMS");
            //Console.WriteLine("\n================ MULTITHREADING PROBLEMS =================");

            await thread.AsyncAwaitExample();
            await thread.TaskWhenAllExample();
            thread.ParallelForExample();
            thread.ThreadCreation();
            thread.ProducerConsumer();
            thread.ConcurrentDictionaryExample();
            thread.SemaphoreExample();
            thread.ThreadSafeCounter();
            await thread.CancellationTokenExample();
            await thread.TaskRunExample();
            await thread.ParallelApiCalls();
            await thread.AsyncFileRead();
            await thread.AsyncDatabaseSimulation();
            thread.ThreadPoolExample();
            thread.LockExample();
            thread.MonitorExample();
            thread.LazyInitialization();
            thread.PerformanceComparison();

            //Console.WriteLine("\n================ COLLECTION PROBLEMS ================\n");
            ConsoleHelper.PrintHeader("COLLECTION PROBLEMS");

            collections.StackExample();
            collections.QueueExample();
            collections.DictionaryLookup();
            collections.HashSetRemoveDuplicates();
            collections.LinkedListTraversal();
            collections.CustomStack();
            collections.CustomQueue();
            collections.DetectDuplicates();
            collections.FrequencyCounter();
            collections.MergeLists();
            collections.FindIntersection();
            collections.FindUnion();
            collections.FindDifference();
            collections.ListToDictionary();
            collections.FlattenNestedCollection();
            collections.SortDictionaryByValue();
            collections.TopFrequentElements();
            collections.ReverseList();
            collections.FindMedian();
            collections.FindMode();

            ConsoleHelper.PrintHeader("DESIGN PATTERNS");
           // Console.WriteLine("\n================ DESIGN PATTERNS ================\n");
            dp.RunAll();

        }

    }
}
