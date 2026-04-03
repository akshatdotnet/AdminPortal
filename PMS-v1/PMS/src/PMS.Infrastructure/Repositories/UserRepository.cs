using Microsoft.EntityFrameworkCore;
using PMS.Application.Interfaces.Repositories;
using PMS.Domain.Entities;
using PMS.Infrastructure.Data;

namespace PMS.Infrastructure.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email)
        => await _context.Users
            .FirstOrDefaultAsync(u =>
                u.Email == email && !u.IsDeleted);
}