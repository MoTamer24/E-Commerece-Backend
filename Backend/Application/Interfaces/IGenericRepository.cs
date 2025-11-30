namespace Application.Interfaces;
using System.Linq.Expressions;
public interface IGenericRepository<T>
{
    public Task<T?> GetByIdAsync(int id);
    public Task<IEnumerable<T>> GetAllAsync();
    public Task<T?> FindAsync(Expression<Func<T, bool>> predicate); // this is a good one 
    public Task AddAsync(T entity);
    public Task AddRangeAsync(IEnumerable<T> entities);
    public void Update(T entity);
    public void Remove(T entity);
    public void RemoveRange(IEnumerable<T> entities);
}