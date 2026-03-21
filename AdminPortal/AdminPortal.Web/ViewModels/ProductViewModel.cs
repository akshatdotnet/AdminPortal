using AdminPortal.Application.Common;
using AdminPortal.Application.DTOs;

namespace AdminPortal.Web.ViewModels;

public class ProductListViewModel
{
    public PagedResult<ProductDto> Products { get; set; } = new();
    public string? SearchQuery { get; set; }
    public string? SelectedCategory { get; set; }
    public List<string> Categories { get; set; } = new();
    public int TotalActive { get; set; }
    public int TotalOutOfStock { get; set; }
}

public class ProductFormViewModel
{
    public ProductDto? Product { get; set; }
    public List<string> Categories { get; set; } = new();
    public bool IsEdit => Product?.Id != Guid.Empty;
}
