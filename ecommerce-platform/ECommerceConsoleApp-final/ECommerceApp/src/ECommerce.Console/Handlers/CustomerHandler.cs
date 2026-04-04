using ECommerce.Application.Common.Models;
using ECommerce.Console.Services;
using ECommerce.Domain.Interfaces;

namespace ECommerce.Console.Handlers;

/// <summary>Handles customer selection for demo purposes.</summary>
public class CustomerHandler(ICustomerRepository customers)
{
    // Pre-seeded demo customers (populated after seeding)
    public List<(Guid Id, string Name, string Email)> DemoCustomers { get; } = [];

    public async Task LoadDemoCustomersAsync(CancellationToken ct)
    {
        // We'll load all seeded customers and cache them
        // In real app the user would log in
        DemoCustomers.Clear();
        var all = await customers.GetByIdAsync(Guid.Empty, ct); // placeholder — see SelectCustomerAsync
    }

    public (Guid Id, string Name) SelectCustomer(List<(Guid Id, string Name, string Email)> list)
    {
        ConsoleDisplayService.Prompt("\n  Select customer (demo login)");
        for (int i = 0; i < list.Count; i++)
            System.Console.WriteLine($"    [{i + 1}]  {list[i].Name}  ({list[i].Email})");

        ConsoleDisplayService.Prompt("Enter number");
        if (int.TryParse(ConsoleDisplayService.ReadLine(), out var idx) && idx >= 1 && idx <= list.Count)
            return (list[idx - 1].Id, list[idx - 1].Name);

        return (list[0].Id, list[0].Name);
    }
}
