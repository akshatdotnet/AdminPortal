using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities;

public class OrderItem : Entity
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public Money UnitPrice { get; private set; } = Money.Zero;
    public int Quantity { get; private set; }
    public Money Subtotal => UnitPrice.Multiply(Quantity);

    private OrderItem() { }

    public static OrderItem Create(Guid orderId, CartItem cartItem)
    {
        return new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductId = cartItem.ProductId,
            ProductName = cartItem.ProductName,
            // Fresh Money instance — never share a Value Object reference across entity boundaries
            UnitPrice = new Money(cartItem.UnitPrice.Amount, cartItem.UnitPrice.Currency),
            Quantity = cartItem.Quantity
        };
    }
}
