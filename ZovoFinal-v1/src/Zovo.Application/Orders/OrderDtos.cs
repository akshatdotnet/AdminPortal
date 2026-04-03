using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zovo.Application.Orders
{
    public class OrderDtos
    {

        public class CreateOrderDto
        {
            public int CustomerId { get; set; }
            public string? Notes { get; set; }
            public List<CreateOrderItemDto> Items { get; set; } = [];
        }

        public class CreateOrderItemDto
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal Discount { get; set; }
        }



    }
}
