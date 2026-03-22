using FluentAssertions;
using Order.Domain.Entities;
using Xunit;

namespace Order.Tests.Unit;

public sealed class OrderAggregateTests
{
    private static Order.Domain.Entities.Order MakeOrder() =>
        Order.Domain.Entities.Order.Create(Guid.NewGuid(), new ShippingAddress
        {
            FullName = "Test User", Street = "123 Test St",
            City = "NYC", State = "NY", PostalCode = "10001",
            Country = "US", Phone = "+1234567890"
        });

    [Fact]
    public void Create_ShouldGenerateOrderNumberAndRaiseDomainEvent()
    {
        var o = MakeOrder();
        o.OrderNumber.Should().StartWith("ORD-");
        o.Status.Should().Be(OrderStatus.Pending);
        o.DomainEvents.Should().ContainSingle(e => e is OrderPlacedEvent);
    }

    [Fact]
    public void AddItem_ShouldUpdateTotals()
    {
        var o = MakeOrder();
        o.AddItem(Guid.NewGuid(), "MacBook", "MBP14", 999.99m, 2);
        o.Subtotal.Should().Be(1999.98m);
        o.Items.Should().HaveCount(1);
    }

    [Fact]
    public void AddItem_SameProduct_ShouldMergeQuantity()
    {
        var o = MakeOrder();
        var pid = Guid.NewGuid();
        o.AddItem(pid, "Phone", "SKU1", 100m, 1);
        o.AddItem(pid, "Phone", "SKU1", 100m, 2);
        o.Items.Should().HaveCount(1);
        o.Items.First().Quantity.Should().Be(3);
    }

    [Fact]
    public void ApplyCoupon_ShouldReduceTotal()
    {
        var o = MakeOrder();
        o.AddItem(Guid.NewGuid(), "P", "S", 200m, 1);
        o.SetShipping(10m);
        o.SetTax(16m);
        o.ApplyCoupon("SAVE50", 50m);
        o.Total.Should().Be(176m);
    }

    [Fact]
    public void ConfirmPayment_ShouldTransitionToConfirmed()
    {
        var o = MakeOrder();
        o.AddItem(Guid.NewGuid(), "P", "S", 100m, 1);
        o.ConfirmPayment("pi_test_123");
        o.Status.Should().Be(OrderStatus.Confirmed);
        o.PaymentStatus.Should().Be(PaymentStatus.Paid);
        o.DomainEvents.Should().Contain(e => e is OrderConfirmedEvent);
    }

    [Fact]
    public void Cancel_AfterShip_ShouldThrow()
    {
        var o = MakeOrder();
        o.AddItem(Guid.NewGuid(), "P", "S", 100m, 1);
        o.ConfirmPayment("pi_123");
        o.StartProcessing();
        o.Ship("TRK123", "UPS");
        o.Invoking(x => x.Cancel("late")).Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*Invalid transition*");
    }

    [Fact]
    public void FullLifecycle_ShouldWorkCorrectly()
    {
        var o = MakeOrder();
        o.AddItem(Guid.NewGuid(), "P", "S", 100m, 1);
        o.ConfirmPayment("pi_123");
        o.StartProcessing();
        o.Ship("TRK456", "FedEx");
        o.Deliver();
        o.Status.Should().Be(OrderStatus.Delivered);
        o.TrackingNumber.Should().Be("TRK456");
        o.StatusHistory.Should().HaveCount(5);
    }
}
