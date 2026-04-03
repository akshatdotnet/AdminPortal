using STHEnterprise.Blazor.Data;
using STHEnterprise.Blazor.Models;

namespace STHEnterprise.Blazor.Services
{
    public class ProductService
    {
        private readonly MockDatabase _db;

        public ProductService(MockDatabase db)
        {
            _db = db;
        }

        public List<Product> GetProducts()
        {
            return _db.Products;
        }
    }
}