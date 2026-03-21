using AdminPortal.Application.Common;
using AdminPortal.Application.DTOs;
using AdminPortal.Domain.Entities;

namespace AdminPortal.Web.ViewModels;

public class AudienceViewModel
{
    public PagedResult<CustomerDto> Customers { get; set; } = new();
    public string? SearchQuery { get; set; }
    public CustomerType? SelectedType { get; set; }
    public int AllCount { get; set; }
    public int NewCount { get; set; }
    public int ReturningCount { get; set; }
    public int ImportedCount { get; set; }
}
