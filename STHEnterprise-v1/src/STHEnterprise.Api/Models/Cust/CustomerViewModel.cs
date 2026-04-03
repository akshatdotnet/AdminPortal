namespace STHEnterprise.Api.Models.Cust
{
    public class CustomerViewModel
    {
        public int Id { get; set; }

        public string CompanyName { get; set; }
        public string BillToParty { get; set; }
        public string ShipToParty { get; set; }

        public string ContactName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        public string CustomerType { get; set; }
        public bool IsActive { get; set; }
    }

    public class CustomerQueryParameters
    {
        public string? Search { get; set; }
        public string? CustomerType { get; set; }
        public bool? IsActive { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class PagedResponse<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public IEnumerable<T> Data { get; set; }
    }



}
