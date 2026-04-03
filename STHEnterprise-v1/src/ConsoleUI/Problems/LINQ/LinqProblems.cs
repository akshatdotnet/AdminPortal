using ConsoleUI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleUI.Problems.LINQ
{
    /*
     1 Group employees by department
     2 Find highest salary
     3 Top 3 salaries
     4 Remove duplicates
     5 Order employees by salary
     6 Convert list to dictionary
     7 Count employees per department
     8 Find employee by name
     9 Find average salary
     10 Find employees earning above average
     11 Join two collections
     12 Distinct values
     13 Select projection
     14 Flatten nested list
     15 Find duplicate records
     16 Paging using Skip Take
     17 Sort multiple columns
     18 Filter using Where
     19 Aggregate example
     20 Group by multiple columns 
     */


    #region LINQ

    public class LinqProblems
    {
        private List<Employee> employees = new()
    {
        new Employee{Id=1,Name="John",Department="IT",Salary=60000},
        new Employee{Id=2,Name="Sara",Department="HR",Salary=50000},
        new Employee{Id=3,Name="Mike",Department="IT",Salary=75000},
        new Employee{Id=4,Name="David",Department="Finance",Salary=65000},
        new Employee{Id=5,Name="Emma",Department="HR",Salary=48000},
        new Employee{Id=6,Name="Alex",Department="IT",Salary=80000}
    };

        public void GroupByDepartment()
        {
            Console.WriteLine("1 Group By Department");
            var g = employees.GroupBy(e => e.Department);
            foreach (var d in g)
                Console.WriteLine($"{d.Key}:{d.Count()}");
        }

        public void HighestSalary() =>
            Console.WriteLine("2 Highest Salary: " + employees.Max(x => x.Salary));

        public void Top3Salaries()
        {
            Console.WriteLine("3 Top 3 Salaries");
            employees.OrderByDescending(x => x.Salary).Take(3)
                .ToList().ForEach(x => Console.WriteLine($"{x.Name}-{x.Salary}"));
        }

        public void RemoveDuplicates() =>
            Console.WriteLine("4 Remove Duplicates: " + string.Join(",", new[] { 1, 2, 2, 3 }.Distinct()));

        public void OrderBySalary()
        {
            Console.WriteLine("5 Order By Salary");
            employees.OrderBy(x => x.Salary)
                .ToList().ForEach(x => Console.WriteLine($"{x.Name}-{x.Salary}"));
        }

        public void ConvertToDictionary()
        {
            Console.WriteLine("6 To Dictionary");
            var d = employees.ToDictionary(x => x.Id, x => x.Name);
            foreach (var x in d) Console.WriteLine($"{x.Key}:{x.Value}");
        }

        public void CountPerDepartment() =>
            Console.WriteLine("7 Count Per Dept: " +
                string.Join(",", employees.GroupBy(x => x.Department)
                .Select(g => $"{g.Key}:{g.Count()}")));

        public void FindEmployeeByName() =>
            Console.WriteLine("8 Find By Name: " +
                employees.FirstOrDefault(x => x.Name == "John")?.Name);

        public void AverageSalary() =>
            Console.WriteLine("9 Avg Salary: " + employees.Average(x => x.Salary));

        public void AboveAverageSalary()
        {
            Console.WriteLine("10 Above Avg");
            var avg = employees.Average(x => x.Salary);
            employees.Where(x => x.Salary > avg)
                .ToList().ForEach(x => Console.WriteLine(x.Name));
        }

        public void JoinCollections()
        {
            Console.WriteLine("11 Join Collections");
            var depts = new[] { "IT", "HR", "Finance" };
            var r = employees.Join(depts, e => e.Department, d => d, (e, d) => e.Name + "-" + d);
            foreach (var x in r) Console.WriteLine(x);
        }

        public void DistinctDepartments() =>
            Console.WriteLine("12 Distinct Dept: " + string.Join(",", employees.Select(x => x.Department).Distinct()));

        public void SelectProjection()
        {
            Console.WriteLine("13 Select Projection");
            employees.Select(x => new { x.Name, x.Salary })
                .ToList().ForEach(x => Console.WriteLine($"{x.Name}-{x.Salary}"));
        }

        public void FlattenNestedList() =>
            Console.WriteLine("14 Flatten: " + string.Join(",", new List<List<int>> { new() { 1, 2 }, new() { 3, 4 } }.SelectMany(x => x)));

        public void FindDuplicateNumbers() =>
            Console.WriteLine("15 Duplicates: " +
                string.Join(",", new[] { 1, 2, 2, 3, 3 }.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key)));

        public void PagingExample()
        {
            Console.WriteLine("16 Paging");
            employees.Skip(2).Take(2).ToList().ForEach(x => Console.WriteLine(x.Name));
        }

        public void SortMultipleColumns()
        {
            Console.WriteLine("17 Sort Multi Columns");
            employees.OrderBy(x => x.Department).ThenByDescending(x => x.Salary)
                .ToList().ForEach(x => Console.WriteLine($"{x.Department}-{x.Name}-{x.Salary}"));
        }

        public void FilterExample()
        {
            Console.WriteLine("18 Filter Salary > 60000");
            employees.Where(x => x.Salary > 60000)
                .ToList().ForEach(x => Console.WriteLine(x.Name));
        }

        public void AggregateSum() =>
            Console.WriteLine("19 Aggregate Sum: " + new[] { 1, 2, 3, 4 }.Aggregate((a, b) => a + b));

        public void GroupByMultipleColumns()
        {
            Console.WriteLine("20 Group By Multi");
            var g = employees.GroupBy(x => new { x.Department, x.Salary });
            foreach (var x in g)
                Console.WriteLine($"{x.Key.Department}-{x.Key.Salary}:{x.Count()}");
        }
    }

    #endregion


    
    
 
}
