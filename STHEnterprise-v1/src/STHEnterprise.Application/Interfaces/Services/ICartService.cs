using STHEnterprise.Application.DTOs;

namespace STHEnterprise.Application.Interfaces;

public interface ICartService
{
    CartDto GetCart();
    CartDto AddItem(int productId, int quantity);
    CartDto UpdateItem(int productId, int quantity);
    void RemoveItem(int productId);
    void Clear();
}
