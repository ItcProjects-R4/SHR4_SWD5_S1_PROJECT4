namespace EventifyPro.DAL.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly EventifyDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(EventifyDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public IQueryable<T> GetQuery()
        => _dbSet.AsQueryable();

    public async Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        => await _dbSet.FindAsync([id], cancellationToken);

    public void SoftDelete(T entity)
    {
        if (entity is ISoftDelete softDeleteEntity)
        {
            softDeleteEntity.IsDeleted = true;
            _context.Entry(entity).State = EntityState.Modified;
        }
        else
        {
            throw new InvalidOperationException($"Entity of type {typeof(T).Name} does not implement ISoftDelete.");
        }
    }

    public async Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await _dbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await _dbSet.AddAsync(entity, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        => await _dbSet.AddRangeAsync(entities, cancellationToken);

    public void Update(T entity)
        => _dbSet.Update(entity);

    public void Delete(T entity)
        => _dbSet.Remove(entity);

    public void DeleteRange(IEnumerable<T> entities)
        => _dbSet.RemoveRange(entities);

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
        => await _dbSet.CountAsync(cancellationToken);

    public async Task<int> CountAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await _dbSet.CountAsync(predicate, cancellationToken);

    public async Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(predicate, cancellationToken);

    public async Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate, cancellationToken);

    public async Task<PagedResult<T>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking();
        return await query.ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
    }

    public async Task<PagedResult<T>> GetPagedAsync(
        Expression<Func<T, bool>> predicate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().Where(predicate);
        return await query.ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
    }
}