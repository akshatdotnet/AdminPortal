using STHEnterprise.Api.Models.Cust;

namespace STHEnterprise.Api.Data
{
    public static class CustomerStore
    {
        public static List<CustomerViewModel> Customers = new()
    {
        new CustomerViewModel
        {
            Id = 1,
            CompanyName = "KVN Logistics",
            BillToParty = "KVN Billing",
            ShipToParty = "KVN Shipping",
            ContactName = "Hardik Patel",
            Email = "hardik@kvn.com",
            Phone = "9022290999",
            CustomerType = "T",
            IsActive = true
        },
        new CustomerViewModel
        {
            Id = 2,
            CompanyName = "ABC Shipping",
            BillToParty = "ABC Billing",
            ShipToParty = "ABC Shipping",
            ContactName = "Amit Shah",
            Email = "amit@abc.com",
            Phone = "9876543210",
            CustomerType = "AU",
            IsActive = false
        }
    };
    }

}
