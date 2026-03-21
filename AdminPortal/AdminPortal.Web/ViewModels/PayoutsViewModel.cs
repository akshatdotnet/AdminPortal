using AdminPortal.Application.DTOs;

namespace AdminPortal.Web.ViewModels;

public class PayoutsViewModel
{
    public PayoutSummaryDto Summary { get; set; } = new();
}
