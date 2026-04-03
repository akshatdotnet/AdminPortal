using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using STHEnterprise.Api.Data;
using STHEnterprise.Api.Models.Cust;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/customers")]
public class CustomerController : ControllerBase
{
    private readonly List<CustomerViewModel> _customers = CustomerStore.Customers;

    // =========================
    // GET: List (Paging + Search + Filter)
    // =========================
    [HttpGet]
    public IActionResult GetCustomers([FromQuery] CustomerQueryParameters query)
    {
        var data = _customers.AsQueryable();

        // Search
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            data = data.Where(x =>
                x.CompanyName.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                x.ContactName.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                x.Email.Contains(query.Search, StringComparison.OrdinalIgnoreCase));
        }

        // Filter
        if (!string.IsNullOrWhiteSpace(query.CustomerType))
            data = data.Where(x => x.CustomerType == query.CustomerType);

        if (query.IsActive.HasValue)
            data = data.Where(x => x.IsActive == query.IsActive);

        var totalRecords = data.Count();

        var pagedData = data
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return Ok(new PagedResponse<CustomerViewModel>
        {
            Page = query.Page,
            PageSize = query.PageSize,
            TotalRecords = totalRecords,
            Data = pagedData
        });
    }

    // =========================
    // GET: By Id
    // =========================
    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var customer = _customers.FirstOrDefault(x => x.Id == id);
        if (customer == null)
            return NotFound();

        return Ok(customer);
    }

    // =========================
    // POST: Create
    // =========================
    [HttpPost]
    public IActionResult Create(CustomerViewModel model)
    {
        model.Id = _customers.Max(x => x.Id) + 1;
        _customers.Add(model);

        return CreatedAtAction(nameof(GetById),
            new { id = model.Id }, model);
    }

    // =========================
    // PUT: Update
    // =========================
    [HttpPut("{id:int}")]
    public IActionResult Update(int id, CustomerViewModel model)
    {
        var existing = _customers.FirstOrDefault(x => x.Id == id);
        if (existing == null)
            return NotFound();

        existing.CompanyName = model.CompanyName;
        existing.BillToParty = model.BillToParty;
        existing.ShipToParty = model.ShipToParty;
        existing.ContactName = model.ContactName;
        existing.Email = model.Email;
        existing.Phone = model.Phone;
        existing.CustomerType = model.CustomerType;
        existing.IsActive = model.IsActive;

        return NoContent();
    }

    // =========================
    // DELETE: Soft Delete
    // =========================
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var customer = _customers.FirstOrDefault(x => x.Id == id);
        if (customer == null)
            return NotFound();

        customer.IsActive = false; // soft delete
        return NoContent();
    }
}
