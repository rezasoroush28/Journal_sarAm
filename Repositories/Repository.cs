using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using TelegramBotProject.Data;

public class Repository<T> : IRepository<T> where T : class
{
    private readonly JournalDbContext _context;
    private DbSet<T> entities;

    public Repository(JournalDbContext context)
    {
        _context = context;
        entities = context.Set<T>();
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await entities.ToListAsync();
    }

    public async Task<T> GetByIdAsync(int id)
    {
        return await entities.FindAsync(id);
    }

    public async Task AddAsync(T entity)
    {
        await entities.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        entities.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(T entity)
    {
        entities.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public Task<T> GetByIdAsync(object id)
    {
        throw new NotImplementedException();
    }

    public Task SaveChangesAsync()
    {
        throw new NotImplementedException();
    }

    public IQueryable<T> Table => entities;
    public IQueryable<T> TableNoTracking => entities.AsNoTracking();
}
