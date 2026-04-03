using STHEnterprise.Blazor.Data;
using STHEnterprise.Blazor.Models;

namespace STHEnterprise.Blazor.Services
{
    public class OrderService
    {
        private readonly MockDatabase _db;

        public OrderService(MockDatabase db)
        {
            _db = db;
        }

        public List<Order> GetOrders()
        {
            return _db.Orders;
        }
    }
}