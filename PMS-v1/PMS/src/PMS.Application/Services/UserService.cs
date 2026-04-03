//using Microsoft.Extensions.Logging;
using PMS.Application.Interfaces;
using PMS.Application.Interfaces.Services;
using PMS.Domain.Entities;

namespace PMS.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _uow;
    //private readonly ILogger<UserService> _logger;

    public UserService(IUnitOfWork uow)
        //, ILogger<UserService> logger)
    {
        _uow = uow;
       // _logger = logger;
    }

    public async Task<IEnumerable<User>> GetAllActiveAsync()
        => await _uow.Users.FindAsync(u => u.IsActive && !u.IsDeleted);

    public async Task<User?> GetByIdAsync(int id)
        => await _uow.Users.GetByIdAsync(id);
}