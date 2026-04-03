using System;
using System.Collections.Generic;

#region ================= SINGLETON =================
public sealed class Logger
{
    private static readonly Lazy<Logger> _instance =
        new(() => new Logger());

    public static Logger Instance => _instance.Value;
    private Logger() { }

    public void Log(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[LOG] {message}");
        Console.ResetColor();
    }
}
#endregion

#region ================= ENTITIES =================
public class Order
{
    public string Customer { get; set; }
    public List<string> Items { get; set; } = new();
    public decimal Total { get; set; }
    public IOrderState State { get; set; }

    public void SetState(IOrderState state)
    {
        State = state;
        State.Handle(this);
    }
}
#endregion

#region ================= BUILDER =================
public class OrderBuilder
{
    private readonly Order _order = new();

    public OrderBuilder ForCustomer(string name)
    {
        _order.Customer = name;
        return this;
    }

    public OrderBuilder AddItem(string item, decimal price)
    {
        _order.Items.Add(item);
        _order.Total += price;
        return this;
    }

    public Order Build()
    {
        return _order;
    }
}
#endregion

#region ================= FACTORY =================
public interface IPayment
{
    void Pay(decimal amount);
}

public class CardPayment : IPayment
{
    public void Pay(decimal amount)
    {
        Logger.Instance.Log($"Paid ₹{amount} using CARD");
    }
}

public class PaymentFactory
{
    public static IPayment Create(string type)
    {
        return type.ToUpper() switch
        {
            "CARD" => new CardPayment(),
            _ => throw new ArgumentException("Invalid payment type")
        };
    }
}
#endregion

#region ================= STRATEGY =================
public interface IDeliveryStrategy
{
    decimal Calculate(decimal total);
}

public class NormalDelivery : IDeliveryStrategy
{
    public decimal Calculate(decimal total) => 40;
}
#endregion

#region ================= DECORATOR =================
public interface IFood
{
    decimal Cost();
}

public class Burger : IFood
{
    public decimal Cost() => 120;
}

public abstract class FoodDecorator : IFood
{
    protected IFood _food;
    protected FoodDecorator(IFood food) => _food = food;
    public abstract decimal Cost();
}

public class CheeseDecorator : FoodDecorator
{
    public CheeseDecorator(IFood food) : base(food) { }
    public override decimal Cost() => _food.Cost() + 30;
}
#endregion

#region ================= OBSERVER =================
public interface IObserver
{
    void Update(string message);
}

public class EmailNotifier : IObserver
{
    public void Update(string message)
    {
        Console.WriteLine($"📧 Email: {message}");
    }
}

public class OrderNotifier
{
    private readonly List<IObserver> _observers = new();

    public void Attach(IObserver observer) => _observers.Add(observer);

    public void Notify(string message)
    {
        foreach (var obs in _observers)
            obs.Update(message);
    }
}
#endregion

#region ================= STATE =================
public interface IOrderState
{
    void Handle(Order order);
}

public class PlacedState : IOrderState
{
    public void Handle(Order order)
    {
        Logger.Instance.Log("Order Placed");
    }
}

public class DeliveredState : IOrderState
{
    public void Handle(Order order)
    {
        Logger.Instance.Log("Order Delivered");
    }
}
#endregion

#region ================= COMMAND =================
public interface ICommand
{
    void Execute();
}

public class PlaceOrderCommand : ICommand
{
    private readonly Order _order;
    public PlaceOrderCommand(Order order) => _order = order;

    public void Execute()
    {
        _order.SetState(new PlacedState());
    }
}
#endregion

#region ================= FACADE =================
public class OrderFacade
{
    private readonly OrderNotifier _notifier = new();

    public OrderFacade()
    {
        _notifier.Attach(new EmailNotifier());
    }

    public void PlaceOrder(Order order, string paymentType)
    {
        Logger.Instance.Log("Processing order...");

        // Command
        ICommand placeOrder = new PlaceOrderCommand(order);
        placeOrder.Execute();

        // Factory
        IPayment payment = PaymentFactory.Create(paymentType);
        payment.Pay(order.Total);

        // Observer
        _notifier.Notify("Your order is confirmed!");

        // State Change
        order.SetState(new DeliveredState());
    }
}
#endregion

#region ================= ITERATOR =================
public class OrderHistory
{
    private readonly List<Order> _orders = new();

    public void Add(Order order) => _orders.Add(order);

    public IEnumerable<Order> GetOrders()
    {
        foreach (var order in _orders)
            yield return order;
    }
}
#endregion

#region ================= CONSOLE UI =================
class Program
{
    static void Main()
    {
        Console.WriteLine("🍔 ONLINE FOOD ORDERING SYSTEM 🍔\n");

        // Builder + Decorator
        IFood food = new CheeseDecorator(new Burger());
        decimal foodCost = food.Cost();

        Order order = new OrderBuilder()
            .ForCustomer("Arjun")
            .AddItem("Cheese Burger", foodCost)
            .Build();

        // Strategy
        IDeliveryStrategy delivery = new NormalDelivery();
        order.Total += delivery.Calculate(order.Total);

        // Facade
        OrderFacade facade = new OrderFacade();
        facade.PlaceOrder(order, "CARD");

        // Iterator
        OrderHistory history = new();
        history.Add(order);

        Console.WriteLine("\n📜 ORDER HISTORY");
        foreach (var o in history.GetOrders())
        {
            Console.WriteLine($"{o.Customer} ordered {string.Join(",", o.Items)} - ₹{o.Total}");
        }

        Console.WriteLine("\n✔ Order Flow Completed Successfully");
    }
}
#endregion


