using AdminPortal.Application.DTOs;

namespace AdminPortal.Web.ViewModels;

public class SettingsViewModel
{
    public StoreDto Store { get; set; } = new();
    public string ActiveTab { get; set; } = "store-details";
    public List<StaffDto> Staff { get; set; } = new();
}
