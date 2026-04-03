using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleUI.Utilities
{
    public static class ConsoleHelper
    {
        public static void PrintHeader(string title)
        {
            Console.WriteLine("\n===============================");
            Console.WriteLine(title);
            Console.WriteLine("===============================");
        }
    }
}
