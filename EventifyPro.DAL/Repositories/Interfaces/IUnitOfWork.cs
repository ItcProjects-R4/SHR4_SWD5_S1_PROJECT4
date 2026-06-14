namespace EventifyPro.DAL.Repositories.Interfaces;

/// <summary>
/// Unit of Work pattern interface that aggregates all repository instances.
/// Manages a single transaction scope and ensures all repositories work with the same DbContext.
/// </summary>
/// <remarks>
/// The Unit of Work pattern coordinates between multiple repositories and provides
/// a single SaveChangesAsync() method to commit all changes atomically.
/// 
/// Usage example:
/// <code>
/// using var unitOfWork = new UnitOfWork(dbContext);
/// var booking = await unitOfWork.Bookings.GetByIdAsync(id);
/// booking.Status = BookingStatus.Confirmed;
/// await unitOfWork.Bookings.UpdateAsync(booking);
/// await unitOfWork.SaveChangesAsync();
/// </code>
/// </remarks>
public interface IUnitOfWork : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Gets the repository for Event entities.
    /// </summary>
    IEventRepository Events { get; }

    /// <summary>
    /// Gets the repository for Booking entities.
    /// </summary>
    IBookingRepository Bookings { get; }

    /// <summary>
    /// Gets the repository for Ticket entities.
    /// </summary>
    ITicketRepository Tickets { get; }

    /// <summary>
    /// Gets the repository for Category entities.
    /// </summary>
    ICategoryRepository Categories { get; }

    /// <summary>
    /// Gets the repository for TicketType entities.
    /// </summary>
    ITicketTypeRepository TicketTypes { get; }

    /// <summary>
    /// Gets the repository for BookingItem entities.
    /// </summary>
    IBookingItemRepository BookingItems { get; }

    /// <summary>
    /// Gets the repository for Payment entities.
    /// </summary>
    IPaymentRepository Payments { get; }

    /// <summary>
    /// Gets the repository for Refund entities.
    /// </summary>
    IRefundRepository Refunds { get; }

    /// <summary>
    /// Gets the repository for Review entities.
    /// </summary>
    IReviewRepository Reviews { get; }

    /// <summary>
    /// Gets the repository for WaitingList entities.
    /// </summary>
    IWaitingListRepository WaitingLists { get; }

    /// <summary>
    /// Gets the repository for ScanLog entities.
    /// </summary>
    IScanLogRepository ScanLogs { get; }

    /// <summary>
    /// Gets the repository for OutboxMessage entities.
    /// </summary>
    IOutboxMessageRepository OutboxMessages { get; }

    /// <summary>
    /// Gets the repository for ApplicationUser entities.
    /// </summary>
    IGenericRepository<ApplicationUser> Users { get; }

    /// <summary>
    /// Saves all changes made in all repositories to the database in a single transaction.
    /// If any change fails, all changes are rolled back.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    /// <exception cref="DbUpdateException">Thrown if the save operation fails.</exception>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a database transaction.
    /// Use this when you need explicit transaction control beyond the normal SaveChangesAsync flow.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The database transaction object.</returns>
    /// <remarks>
    /// You should explicitly call CommitAsync() or RollbackAsync() on the returned transaction,
    /// or use it with a using statement to ensure proper disposal.
    /// </remarks>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back all pending changes without saving to the database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the underlying DbContext instance.
    /// Use with caution - direct DbContext access should be minimal.
    /// </summary>
    EventifyDbContext DbContext { get; }
}
