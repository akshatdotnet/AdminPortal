using AdminPortal.Application.DTOs;

namespace AdminPortal.Web.ViewModels;

public class DiscountsViewModel
{
    public List<DiscountDto> Discounts { get; set; } = new();
    public int TotalActive { get; set; }
    public int TotalExpired { get; set; }
}
