using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TelegramBotProject.Data;

public class Repository<T> : IRepository<T> where T : class
{
    private readonly JournalDbContext _context;
    private DbSet<T> _entities;

    public Repository(JournalDbContext context)
    {
        _context = context;
        _entities = context.Set<T>();
    }

    public IQueryable<T> Table => _entities;

    public IQueryable<T> TableNoTracking => _entities.AsNoTracking();

    public async Task<T> GetByIdAsync(object id)
    {
        return await _entities.FindAsync(id);
    }

    public async Task AddAsync(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        await _entities.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _entities.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _entities.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
