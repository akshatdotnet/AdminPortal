using STHEnterprise.Blazor.Models;

namespace STHEnterprise.Blazor.Data
{
    public class MockDatabase
    {
        public List<User> Users = new()
        {
            new User
            {
                Email="admin@sth.com",
                Password="admin123",
                Name="Super Admin",
                Role="SuperAdmin"
            }
        };

        public List<Product> Products = new()
        {
            new Product{Id=1,Name="Laptop",Price=75000,Stock=12},
            new Product{Id=2,Name="Mobile",Price=25000,Stock=25},
            new Product{Id=3,Name="Headphones",Price=3000,Stock=50}
        };

        public List<Order> Orders = new()
        {
            new Order{Id=1001,Customer="Rahul",Total=15000,Status="Completed"},
            new Order{Id=1002,Customer="Amit",Total=2500,Status="Pending"}
        };
    }
}