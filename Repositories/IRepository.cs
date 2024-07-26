using System.Linq;
using System.Linq.Expressions;

public interface IRepository<T> where T : class
{
    IQueryable<T> Table { get; }
    IQueryable<T> TableNoTracking { get; }

    Task<T> GetByIdAsync(object id);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task SaveChangesAsync();
}
