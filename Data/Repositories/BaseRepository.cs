using Data.Contexts;
using Data.Models;
using Domain.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Data.Repositories;

/// <summary>
/// Persistence for <typeparamref name="TEntity"/>. Methods that return <typeparamref name="TModel"/> map from the entity
/// using <see cref="MapExtensions"/> (<c>MapTo&lt;TModel&gt;()</c>) so DTO shape stays in the Domain layer.
/// </summary>
public abstract class BaseRepository<TEntity, TModel> : IBaseRepository<TEntity, TModel> where TEntity : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<TEntity> _table;

    public BaseRepository(AppDbContext context)
    {
        _context = context;
        _table = _context.Set<TEntity>();
    }

    public virtual async Task<RepositoryResult<bool>> AddAsync(TEntity entity)
    {
        if (entity == null)
            return new RepositoryResult<bool> { Succeeded = false, StatusCode = 400, ErrorMessage = "Entity cannot be null." };
        try
        {
            await _table.AddAsync(entity);
            await _context.SaveChangesAsync();
            return new RepositoryResult<bool> { Succeeded = true, StatusCode = 201 };
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return new RepositoryResult<bool> { Succeeded = false, StatusCode = 500, ErrorMessage = ex.Message };
        }
    }

    /// Loads rows (optional filter, includes, sort), then projects with <paramref name="selector"/>; use <c>e => e</c> to materialize entities.
    /// <paramref name="selector"/> must be translatable to SQL by EF Core (client-only calls such as <c>MapTo</c> will fail at runtime).
    /// <param name="where">Filter predicate; must return <see cref="bool"/> (e.g. <c>e => e.EventId == id</c>). Not a property selector.</param>
    public virtual async Task<RepositoryResult<IEnumerable<TSelect>>> GetAllAsync<TSelect>(Expression<Func<TEntity, TSelect>> selector, bool orderBydescending = false, Expression<Func<TEntity, object>>? sortBy = null, Expression<Func<TEntity, bool>>? where = null, params Expression<Func<TEntity, object>>[] includes)
    {
        IQueryable<TEntity> query = _table;

        if (where != null)
            query = query.Where(where);

        if (includes != null && includes.Length != 0)
            foreach (var include in includes)
                query = query.Include(include);

        if (sortBy != null)
            query = orderBydescending
                ? query.OrderByDescending(sortBy)
                : query.OrderBy(sortBy);

        var result = await query.Select(selector).ToListAsync();
        return new RepositoryResult<IEnumerable<TSelect>> { Succeeded = true, StatusCode = 200, Result = result };
    }

    /// Finds one entity and maps it to <typeparamref name="TModel"/> using
    /// <see cref="MapExtensions.MapTo{TDestination}(object)"/>; returns 404 when no row matches <paramref name="findBy"/>.
    public virtual async Task<RepositoryResult<TModel>> GetAsync(Expression<Func<TEntity, bool>> where, params Expression<Func<TEntity, object>>[] includes)
    {
        IQueryable<TEntity> query = _table;

        if (includes != null && includes.Length != 0)
            foreach (var include in includes)
                query = query.Include(include);

        var entity = await query.FirstOrDefaultAsync(where);
        if (entity == null)
            return new RepositoryResult<TModel> { Succeeded = false, StatusCode = 404, ErrorMessage = "Entity not found." };
        var result = entity.MapTo<TModel>();
        return new RepositoryResult<TModel> { Succeeded = true, StatusCode = 200, Result = result };
    }

    public virtual async Task<RepositoryResult<bool>> ExistsAsync(Expression<Func<TEntity, bool>> findBy)
    {
        var exists = await _table.AnyAsync(findBy);
        return exists
            ? new RepositoryResult<bool> { Succeeded = true, StatusCode = 200 }
            : new RepositoryResult<bool> { Succeeded = false, StatusCode = 404, ErrorMessage = "Entity not found." };
    }

    public async Task<RepositoryResult<bool>> UpdateAsync(TEntity entity)
    {
        if (entity == null)
            return new RepositoryResult<bool> { Succeeded = false, StatusCode = 400, ErrorMessage = "Entity cannot be null." };

        try
        {
            _table.Update(entity);
            await _context.SaveChangesAsync();
            return new RepositoryResult<bool> { Succeeded = true, StatusCode = 200 };
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return new RepositoryResult<bool> { Succeeded = false, StatusCode = 500, ErrorMessage = ex.Message };
        }
    }

    public async Task<RepositoryResult<bool>> DeleteAsync(TEntity entity)
    {
        if (entity == null)
            return new RepositoryResult<bool> { Succeeded = false, StatusCode = 400, ErrorMessage = "Entity cannot be null." };

        try
        {
            _table.Remove(entity);
            await _context.SaveChangesAsync();
            return new RepositoryResult<bool> { Succeeded = true, StatusCode = 200 };
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return new RepositoryResult<bool> { Succeeded = false, StatusCode = 500, ErrorMessage = ex.Message };
        }
    }
}
