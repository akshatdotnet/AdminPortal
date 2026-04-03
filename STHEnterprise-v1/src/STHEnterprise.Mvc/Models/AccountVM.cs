namespace STHEnterprise.Mvc.Models
{
    public class AccountVM
    {
        public string Phone { get; set; } = "";
        public List<OrderVM> Orders { get; set; } = new();
    }

    public class OrderVM
    {
        public string Store { get; set; } = "";
        public string OrderNo { get; set; } = "";
        public decimal Amount { get; set; }
        public string Status { get; set; } = "";
        public DateTime Date { get; set; }
    }

}
