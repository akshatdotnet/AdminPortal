using AdminPortal.Application.Common;
using AdminPortal.Application.DTOs;
using AdminPortal.Domain.Entities;

namespace AdminPortal.Web.ViewModels;

public class OrdersViewModel
{
    public PagedResult<OrderDto> Orders { get; set; } = new();
    public OrderStatus? SelectedStatus { get; set; }
    public string? SearchQuery { get; set; }
    public int PendingCount { get; set; }
    public int ProcessingCount { get; set; }
    public int DeliveredCount { get; set; }
    public int CancelledCount { get; set; }
}
