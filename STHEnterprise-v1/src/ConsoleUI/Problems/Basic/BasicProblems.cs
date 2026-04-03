using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleUI.Problems.Basic
{
    /*
        1 Reverse string
        2 Palindrome check
        3 Duplicate characters
        4 Character frequency
        5 Second largest number
        6 Swap numbers
        7 Fibonacci series
        8 Prime number
        9 Factorial recursion
        10 Armstrong number
        11 Missing number
        12 Sum of digits
        13 Reverse number
        14 Even/Odd filter
        15 Max number in array
        16 Min number in array
        17 Remove duplicates array
        18 Count vowels
        19 Count words in sentence
        20 Sort numbers
     */





    #region BASIC

    public class BasicProblems
    {
        public string ReverseString(string s)
        {
            char[] arr = s.ToCharArray();
            Array.Reverse(arr);
            return new string(arr);
        }

        public bool IsPalindrome(string s)
            => s.Equals(new string(s.Reverse().ToArray()), StringComparison.OrdinalIgnoreCase);

        public IEnumerable<char> GetDuplicateCharacters(string s)
            => s.GroupBy(c => c).Where(g => g.Count() > 1).Select(g => g.Key);

        public Dictionary<char, int> CharacterFrequency(string s)
            => s.GroupBy(c => c).ToDictionary(g => g.Key, g => g.Count());

        public int SecondLargest(int[] arr)
            => arr.Distinct().OrderByDescending(x => x).Skip(1).First();

        public (int, int) SwapNumbers(int a, int b)
        {
            a ^= b; b ^= a; a ^= b;
            return (a, b);
        }

        public List<int> Fibonacci(int n)
        {
            var list = new List<int>();
            int a = 0, b = 1;
            for (int i = 0; i < n; i++)
            {
                list.Add(a);
                (a, b) = (b, a + b);
            }
            return list;
        }

        public bool IsPrime(int n)
        {
            if (n <= 1) return false;
            for (int i = 2; i <= Math.Sqrt(n); i++)
                if (n % i == 0) return false;
            return true;
        }

        public int Factorial(int n) => n <= 1 ? 1 : n * Factorial(n - 1);

        public bool IsArmstrong(int n)
        {
            int sum = 0, t = n;
            while (t > 0)
            {
                int d = t % 10;
                sum += d * d * d;
                t /= 10;
            }
            return sum == n;
        }

        public int FindMissingNumber(int[] arr, int n)
            => n * (n + 1) / 2 - arr.Sum();

        public int SumOfDigits(int n)
        {
            int sum = 0;
            while (n > 0) { sum += n % 10; n /= 10; }
            return sum;
        }

        public int ReverseNumber(int n)
        {
            int r = 0;
            while (n > 0) { r = r * 10 + n % 10; n /= 10; }
            return r;
        }

        public (List<int>, List<int>) EvenOddFilter(int[] arr)
            => (arr.Where(x => x % 2 == 0).ToList(), arr.Where(x => x % 2 != 0).ToList());

        public int MaxNumber(int[] arr) => arr.Max();
        public int MinNumber(int[] arr) => arr.Min();
        public int[] RemoveDuplicates(int[] arr) => arr.Distinct().ToArray();
        public int CountVowels(string s) => s.ToLower().Count(c => "aeiou".Contains(c));
        public int CountWords(string s) => s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        public int[] SortNumbers(int[] arr) => arr.OrderBy(x => x).ToArray();
    }

    #endregion
   



}
