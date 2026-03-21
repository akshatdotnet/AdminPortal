using AdminPortal.Application.DTOs;
using AdminPortal.Domain.Entities;

namespace AdminPortal.Web.ViewModels;

public class CreditsViewModel
{
    public CreditSummaryDto Summary { get; set; } = new();
    public string SelectedFilter { get; set; } = "All";
    public string SelectedDateRange { get; set; } = "30d";
}
