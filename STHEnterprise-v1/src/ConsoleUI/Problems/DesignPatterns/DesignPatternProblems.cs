using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleUI.Problems.DesignPatterns
{

    /*
       1 Singleton Logger
       2 Factory Pattern
       3 Repository Pattern
       4 Dependency Injection
       5 Strategy Pattern
       6 Observer Pattern
       7 Builder Pattern
       8 Decorator Pattern
       9 Adapter Pattern
       10 Mediator Pattern
       11 Facade Pattern
       12 Command Pattern
       13 Unit of Work Pattern
       14 CQRS Example
       15 Retry Pattern
       16 Circuit Breaker Simulation
       17 Caching Service
       18 Notification Service
       19 Payment Strategy
       20 Logging Framework
      */

    public class DesignPatternProblems
    {

        public void RunAll()
        {
            
            /* 1 Singleton */
            Logger.Instance.Log("1 Singleton Logger Example");

            /* 2 Factory */
            Console.WriteLine("\n2 Factory Pattern");
            var shape = ShapeFactory.Create("circle");
            shape.Draw();

            /* 3 Repository */
            Console.WriteLine("\n3 Repository Pattern");
            IRepository<string> repo = new MemoryRepository<string>();
            repo.Add("Item1");
            repo.Add("Item2");
            foreach (var item in repo.GetAll())
                Console.WriteLine(item);

            /* 4 Dependency Injection */
            Console.WriteLine("\n4 Dependency Injection");
            IMessageService email = new EmailService();
            Notification notification = new Notification(email);
            notification.Notify("Hello DI");

            /* 5 Strategy Pattern */
            Console.WriteLine("\n5 Strategy Pattern");
            var calc = new PriceCalculator(new PremiumDiscount());
            Console.WriteLine(calc.Calculate(100));

            /* 6 Observer Pattern */
            Console.WriteLine("\n6 Observer Pattern");
            Publisher publisher = new Publisher();
            publisher.Subscribe(new Subscriber());
            publisher.Notify("New Event");

            /* 7 Builder Pattern */
            Console.WriteLine("\n7 Builder Pattern");
            var house = new HouseBuilder()
                .BuildWalls()
                .BuildRoof()
                .Build();
            Console.WriteLine($"{house.Walls} - {house.Roof}");

            /* 8 Decorator Pattern */
            Console.WriteLine("\n8 Decorator Pattern");
            ICoffee coffee = new MilkDecorator(new BasicCoffee());
            Console.WriteLine(coffee.Cost());

            /* 9 Adapter Pattern */
            Console.WriteLine("\n9 Adapter Pattern");
            IPrinter printer = new PrinterAdapter();
            printer.Print();

            /* 10 Mediator */
            Console.WriteLine("\n10 Mediator Pattern");
            ChatMediator mediator = new ChatMediator();
            User user = new User(mediator, "John");
            user.Send("Hello");

            /* 11 Facade */
            Console.WriteLine("\n11 Facade Pattern");
            PaymentFacade facade = new PaymentFacade();
            facade.Pay();

            /* 12 Command */
            Console.WriteLine("\n12 Command Pattern");
            ICommand command = new LightOnCommand();
            command.Execute();

            /* 13 Unit Of Work */
            Console.WriteLine("\n13 Unit Of Work");
            UnitOfWork uow = new UnitOfWork();
            uow.Commit();

            /* 14 CQRS */
            Console.WriteLine("\n14 CQRS");
            var handler = new GetUserHandler();
            Console.WriteLine(handler.Handle(new GetUserQuery { Id = 1 }));

            /* 15 Retry */
            Console.WriteLine("\n15 Retry Pattern");
            RetryExample();

            /* 16 Circuit Breaker */
            Console.WriteLine("\n16 Circuit Breaker");
            CircuitBreakerExample();

            /* 17 Caching */
            Console.WriteLine("\n17 Caching Service");
            CacheService cache = new CacheService();
            Console.WriteLine(cache.Get("user"));

            /* 18 Notification */
            Console.WriteLine("\n18 Notification Service");
            NotificationService service = new NotificationService();
            service.SendEmail("Welcome");
            service.SendSMS("OTP Sent");

            /* 19 Payment Strategy */
            Console.WriteLine("\n19 Payment Strategy");
            IPayment payment = new CreditCardPayment();
            payment.Pay(100);

            /* 20 Logging Framework */
            Console.WriteLine("\n20 Logging Framework");
            FileLogger logger = new FileLogger();
            logger.Write("Application Started");
        }



        /* 1 Singleton */
        public sealed class Logger
        {
            private static readonly Lazy<Logger> instance =
                new Lazy<Logger>(() => new Logger());

            private Logger() { }

            public static Logger Instance => instance.Value;

            public void Log(string message)
            {
                Console.WriteLine($"LOG: {message}");
            }
        }


        /* 2 Factory Pattern */

        interface IShape
        {
            void Draw();
        }

        class Circle : IShape
        {
            public void Draw() => Console.WriteLine("Circle Drawn");
        }

        class Rectangle : IShape
        {
            public void Draw() => Console.WriteLine("Rectangle Drawn");
        }

       class ShapeFactory
        {
            public static IShape Create(string type)
            {
                return type switch
                {
                    "circle" => new Circle(),
                    "rectangle" => new Rectangle(),
                    _ => throw new Exception("Invalid shape")
                };
            }
        }


        /* 3 Repository Pattern */

        interface IRepository<T>
        {
            void Add(T entity);
            IEnumerable<T> GetAll();
        }

        class MemoryRepository<T> : IRepository<T>
        {
            private readonly List<T> _data = new();

            public void Add(T entity) => _data.Add(entity);

            public IEnumerable<T> GetAll() => _data;
        }


        /* 4 Dependency Injection */

        interface IMessageService
        {
            void Send(string message);
        }

        class EmailService : IMessageService
        {
            public void Send(string message)
            {
                Console.WriteLine($"Email Sent: {message}");
            }
        }

        class Notification
        {
            private readonly IMessageService _service;

            public Notification(IMessageService service)
            {
                _service = service;
            }

            public void Notify(string msg)
            {
                _service.Send(msg);
            }
        }


        /* 5 Strategy Pattern */

        interface IDiscountStrategy
        {
            double ApplyDiscount(double price);
        }

        class RegularDiscount : IDiscountStrategy
        {
            public double ApplyDiscount(double price) => price * 0.9;
        }

        class PremiumDiscount : IDiscountStrategy
        {
            public double ApplyDiscount(double price) => price * 0.7;
        }

        class PriceCalculator
        {
            private readonly IDiscountStrategy _strategy;

            public PriceCalculator(IDiscountStrategy strategy)
            {
                _strategy = strategy;
            }

            public double Calculate(double price)
            {
                return _strategy.ApplyDiscount(price);
            }
        }


        /* 6 Observer Pattern */

        interface IObserver
        {
            void Update(string message);
        }

        class Subscriber : IObserver
        {
            public void Update(string message)
            {
                Console.WriteLine($"Received: {message}");
            }
        }

        class Publisher
        {
            private readonly List<IObserver> observers = new();

            public void Subscribe(IObserver observer)
            {
                observers.Add(observer);
            }

            public void Notify(string message)
            {
                foreach (var o in observers)
                    o.Update(message);
            }
        }


        /* 7 Builder Pattern */

        class House
        {
            public string Walls;
            public string Roof;
        }

        class HouseBuilder
        {
            private House house = new House();

            public HouseBuilder BuildWalls()
            {
                house.Walls = "Concrete Walls";
                return this;
            }

            public HouseBuilder BuildRoof()
            {
                house.Roof = "Metal Roof";
                return this;
            }

            public House Build()
            {
                return house;
            }
        }


        /* 8 Decorator Pattern */

        interface ICoffee
        {
            int Cost();
        }

        class BasicCoffee : ICoffee
        {
            public int Cost() => 5;
        }

        class MilkDecorator : ICoffee
        {
            private ICoffee coffee;

            public MilkDecorator(ICoffee coffee)
            {
                this.coffee = coffee;
            }

            public int Cost() => coffee.Cost() + 2;
        }


        /* 9 Adapter Pattern */

        interface IPrinter
        {
            void Print();
        }

        class LegacyPrinter
        {
            public void PrintOld()
            {
                Console.WriteLine("Old Printer");
            }
        }

        class PrinterAdapter : IPrinter
        {
            private LegacyPrinter printer = new LegacyPrinter();

            public void Print()
            {
                printer.PrintOld();
            }
        }


        /* 10 Mediator Pattern */

        class ChatMediator
        {
            public void Send(string message, User user)
            {
                Console.WriteLine($"{user.Name}: {message}");
            }
        }

        class User
        {
            public string Name;
            private ChatMediator mediator;

            public User(ChatMediator mediator, string name)
            {
                this.mediator = mediator;
                Name = name;
            }

            public void Send(string msg)
            {
                mediator.Send(msg, this);
            }
        }


        /* 11 Facade Pattern */

        class PaymentFacade
        {
            public void Pay()
            {
                Console.WriteLine("Validating Card");
                Console.WriteLine("Processing Payment");
                Console.WriteLine("Payment Successful");
            }
        }


        /* 12 Command Pattern */

        interface ICommand
        {
            void Execute();
        }

        class LightOnCommand : ICommand
        {
            public void Execute()
            {
                Console.WriteLine("Light ON");
            }
        }


        /* 13 Unit of Work */

        class UnitOfWork
        {
            public void Commit()
            {
                Console.WriteLine("Transaction Committed");
            }
        }


        /* 14 CQRS */

        class GetUserQuery
        {
            public int Id;
        }

        class GetUserHandler
        {
            public string Handle(GetUserQuery q)
            {
                return $"User {q.Id}";
            }
        }


        /* 15 Retry Pattern */

        public void RetryExample()
        {
            int retries = 3;

            for (int i = 0; i < retries; i++)
            {
                try
                {
                    Console.WriteLine("Trying operation...");
                    throw new Exception();
                }
                catch
                {
                    Console.WriteLine("Retrying...");
                }
            }
        }


        /* 16 Circuit Breaker */

        bool circuitOpen = false;

        public void CircuitBreakerExample()
        {
            if (circuitOpen)
            {
                Console.WriteLine("Circuit Open - Skipping Call");
                return;
            }

            Console.WriteLine("Calling service");
        }


        /* 17 Caching Service */

        class CacheService
        {
            private Dictionary<string, string> cache = new();

            public string Get(string key)
            {
                if (cache.ContainsKey(key))
                    return cache[key];

                string value = "FetchedFromDB";
                cache[key] = value;

                return value;
            }
        }


        /* 18 Notification Service */

        class NotificationService
        {
            public void SendEmail(string msg)
            {
                Console.WriteLine($"Email: {msg}");
            }

            public void SendSMS(string msg)
            {
                Console.WriteLine($"SMS: {msg}");
            }
        }


        /* 19 Payment Strategy */

        interface IPayment
        {
            void Pay(int amount);
        }

        class CreditCardPayment : IPayment
        {
            public void Pay(int amount)
            {
                Console.WriteLine($"Paid {amount} via CreditCard");
            }
        }

        class PayPalPayment : IPayment
        {
            public void Pay(int amount)
            {
                Console.WriteLine($"Paid {amount} via PayPal");
            }
        }


        /* 20 Logging Framework */

        class FileLogger
        {
            public void Write(string message)
            {
                Console.WriteLine($"Writing log to file: {message}");
            }
        }


    }
}