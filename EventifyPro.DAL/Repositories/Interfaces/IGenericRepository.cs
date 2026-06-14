namespace EventifyPro.DAL.Repositories.Interfaces;

/// <summary>
/// Generic repository — provides standard CRUD and query operations.
/// All entity-specific repositories inherit from this interface.
/// </summary>
public interface IGenericRepository<T> where T : class
{
    /// <summary>
    /// Returns an IQueryable for composing complex LINQ queries.
    /// Caller is responsible for calling AsNoTracking() when appropriate.
    /// </summary>
    IQueryable<T> GetQuery();

    /// <summary>
    /// Gets an entity by its primary key using EF Core's identity map (cache-aware).
    /// Returns null if not found.
    /// </summary>
    Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all entities — use with caution on large tables.
    /// Uses AsNoTracking for read performance.
    /// </summary>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Filters entities using a DB-translated expression (SQL WHERE clause).
    /// Uses AsNoTracking for read performance.
    /// </summary>
    Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>Adds a new entity. Changes persist on UnitOfWork.CommitAsync().</summary>
    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>Adds multiple entities in batch.</summary>
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks entity as Modified. Changes persist on UnitOfWork.CommitAsync().
    /// Note: For tracked entities, no explicit call needed — EF Core detects changes.
    /// </summary>
    void Update(T entity);

    /// <summary>
    /// Marks entity for deletion. Changes persist on UnitOfWork.CommitAsync().
    /// </summary>
    void Delete(T entity);

    /// <summary>Marks multiple entities for deletion.</summary>
    void DeleteRange(IEnumerable<T> entities);

    /// <summary>Returns total count of all entities.</summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns count of entities matching predicate — translated to SQL COUNT.</summary>
    Task<int> CountAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>Returns true if any entity matches — translated to SQL EXISTS.</summary>
    Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>Returns first matching entity or null — translated to SQL WHERE + TOP 1.</summary>
    Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns paginated results for all entities using database-level pagination.
    /// Executes COUNT and OFFSET/FETCH at the database level.
    /// </summary>
    /// <param name="pageNumber">Page number starting from 1.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A PagedResult containing page data and metadata.</returns>
    Task<PagedResult<T>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns paginated results for entities matching a predicate using database-level pagination.
    /// Executes COUNT and OFFSET/FETCH at the database level.
    /// </summary>
    /// <param name="predicate">Filter expression.</param>
    /// <param name="pageNumber">Page number starting from 1.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A PagedResult containing filtered page data and metadata.</returns>
    Task<PagedResult<T>> GetPagedAsync(
        Expression<Func<T, bool>> predicate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}