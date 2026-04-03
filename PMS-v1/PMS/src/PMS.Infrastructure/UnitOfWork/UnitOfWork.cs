using PMS.Application.Interfaces;
using PMS.Application.Interfaces.Repositories;
using PMS.Infrastructure.Data;
using PMS.Infrastructure.Dapper;
using PMS.Infrastructure.Repositories;

namespace PMS.Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly DapperContext _dapper;

    private IProjectRepository? _projects;
    private ITaskRepository? _tasks;
    private ITimeLogRepository? _timeLogs;
    private IUserRepository? _users;

    public UnitOfWork(ApplicationDbContext context, DapperContext dapper)
    {
        _context = context;
        _dapper = dapper;
    }

    public IProjectRepository Projects
        => _projects ??= new ProjectRepository(_context, _dapper);

    public ITaskRepository Tasks
        => _tasks ??= new TaskRepository(_context, _dapper);

    public ITimeLogRepository TimeLogs
        => _timeLogs ??= new TimeLogRepository(_context);

    public IUserRepository Users
        => _users ??= new UserRepository(_context);

    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);

    public void Dispose() => _context.Dispose();
}