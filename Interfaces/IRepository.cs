using System.Linq.Expressions;
using MovieRentalApp.Models.DTOs;


namespace MovieRentalApp.Interfaces
{
    public interface IRepository<K, T> where T : class
    {
        Task<T> AddAsync(T entity);
        Task<T?> GetByIdAsync(K id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> UpdateAsync(K id, T entity);
        Task<bool> DeleteAsync(K id);
        Task<IEnumerable<T>> FindAsync(
            Expression<Func<T, bool>> predicate);
        Task<bool> ExistsAsync(K id);

      
        Task<IEnumerable<T>> GetAllWithIncludeAsync(
            params Expression<Func<T, object>>[] includes);

       
        IQueryable<T> GetQueryable();
    }
}