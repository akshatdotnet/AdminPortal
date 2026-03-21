using AdminPortal.Api.DTOs;
using AdminPortal.Api.MockData;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SettingsController : ControllerBase
{
    private readonly MockDataStore _store;

    public SettingsController(MockDataStore store) => _store = store;

    /// <summary>Get store details.</summary>
    [HttpGet("store")]
    public ActionResult<ApiResponse<StoreDto>> GetStore() =>
        Ok(new ApiResponse<StoreDto> { Data = MapStore(_store.Store) });

    /// <summary>Update store details.</summary>
    [HttpPut("store")]
    public ActionResult<ApiResponse<StoreDto>> UpdateStore([FromBody] UpdateStoreRequest request)
    {
        _store.Store.Name = request.Name;
        _store.Store.Description = request.Description;
        _store.Store.Category = request.Category;
        _store.Store.PhoneNumber = request.PhoneNumber;
        _store.Store.Address = request.Address;
        return Ok(new ApiResponse<StoreDto> { Data = MapStore(_store.Store), Message = "Store updated." });
    }

    /// <summary>Toggle store open/closed.</summary>
    [HttpPatch("store/toggle")]
    public ActionResult<ApiResponse<StoreDto>> ToggleStore()
    {
        _store.Store.IsOpen = !_store.Store.IsOpen;
        return Ok(new ApiResponse<StoreDto>
        {
            Data = MapStore(_store.Store),
            Message = $"Store is now {(_store.Store.IsOpen ? "open" : "closed")}."
        });
    }

    /// <summary>Get all staff accounts.</summary>
    [HttpGet("staff")]
    public ActionResult<ApiResponse<IEnumerable<StaffDto>>> GetStaff() =>
        Ok(new ApiResponse<IEnumerable<StaffDto>> { Data = _store.Staff.Select(MapStaff) });

    /// <summary>Get a staff member by ID.</summary>
    [HttpGet("staff/{id:int}")]
    public ActionResult<ApiResponse<StaffDto>> GetStaffById(int id)
    {
        var staff = _store.Staff.FirstOrDefault(s => s.Id == id);
        if (staff is null) return NotFound(new ApiResponse<StaffDto> { Success = false, Message = "Staff not found." });
        return Ok(new ApiResponse<StaffDto> { Data = MapStaff(staff) });
    }

    /// <summary>Add a new staff member.</summary>
    [HttpPost("staff")]
    public ActionResult<ApiResponse<StaffDto>> AddStaff([FromBody] AddStaffRequest request)
    {
        if (_store.Staff.Any(s => s.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)))
            return Conflict(new ApiResponse<StaffDto> { Success = false, Message = "Email already in use." });

        var staff = new StaffAccount
        {
            Id = _store.Staff.Any() ? _store.Staff.Max(s => s.Id) + 1 : 1,
            Name = request.Name,
            Email = request.Email,
            Role = Enum.Parse<StaffRole>(request.Role, ignoreCase: true),
            IsActive = true,
            JoinedAt = DateTime.Now
        };

        _store.Staff.Add(staff);
        return CreatedAtAction(nameof(GetStaffById), new { id = staff.Id },
            new ApiResponse<StaffDto> { Data = MapStaff(staff), Message = "Staff added." });
    }

    /// <summary>Toggle staff active/inactive.</summary>
    [HttpPatch("staff/{id:int}/toggle")]
    public ActionResult<ApiResponse<StaffDto>> ToggleStaff(int id)
    {
        var staff = _store.Staff.FirstOrDefault(s => s.Id == id);
        if (staff is null) return NotFound(new ApiResponse<StaffDto> { Success = false, Message = "Staff not found." });

        staff.IsActive = !staff.IsActive;
        return Ok(new ApiResponse<StaffDto> { Data = MapStaff(staff), Message = $"Staff is now {(staff.IsActive ? "active" : "inactive")}." });
    }

    /// <summary>Remove a staff member.</summary>
    [HttpDelete("staff/{id:int}")]
    public ActionResult<ApiResponse<object>> DeleteStaff(int id)
    {
        var staff = _store.Staff.FirstOrDefault(s => s.Id == id);
        if (staff is null) return NotFound(new ApiResponse<object> { Success = false, Message = "Staff not found." });

        _store.Staff.Remove(staff);
        return Ok(new ApiResponse<object> { Message = "Staff removed." });
    }

    private static StoreDto MapStore(Store s) => new()
    {
        Id = s.Id, Name = s.Name, Description = s.Description,
        Category = s.Category, PhoneNumber = s.PhoneNumber,
        Address = s.Address, IsOpen = s.IsOpen, LogoUrl = s.LogoUrl
    };

    private static StaffDto MapStaff(StaffAccount s) => new()
    {
        Id = s.Id, Name = s.Name, Email = s.Email,
        Role = s.Role.ToString(), IsActive = s.IsActive, JoinedAt = s.JoinedAt
    };
}

public record UpdateStoreRequest(
    string Name, string Description, string Category,
    string PhoneNumber, string Address);

public record AddStaffRequest(string Name, string Email, string Role);
