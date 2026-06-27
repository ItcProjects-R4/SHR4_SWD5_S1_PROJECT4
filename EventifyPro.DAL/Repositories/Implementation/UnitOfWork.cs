namespace EventifyPro.DAL.Repositories.Implementation;

/// <summary>
/// Unit of Work implementation that aggregates all repository instances
/// and manages database transactions.
/// </summary>
/// <remarks>
/// Implements the Unit of Work pattern to coordinate between multiple repositories
/// and provide a single SaveChangesAsync() method for atomic transactions.
/// </remarks>
public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly EventifyDbContext _dbContext;
    private IEventRepository? _eventRepository;
    private IBookingRepository? _bookingRepository;
    private ITicketRepository? _ticketRepository;
    private ICategoryRepository? _categoryRepository;
    private ITicketTypeRepository? _ticketTypeRepository;
    private IBookingItemRepository? _bookingItemRepository;
    private IPaymentRepository? _paymentRepository;
    private IRefundRepository? _refundRepository;
    private IReviewRepository? _reviewRepository;
    private IWaitingListRepository? _waitingListRepository;
    private IScanLogRepository? _scanLogRepository;
    private IOutboxMessageRepository? _outboxMessageRepository;
    private ISavedEventRepository? _savedEventRepository;
    private INotificationRepository? _notificationRepository;
    private IGenericRepository<ApplicationUser>? _userRepository;
    private IGenericRepository<OrganizerProfile>? _organizerProfileRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public UnitOfWork(EventifyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Gets the repository for Event entities.
    /// </summary>
    public IEventRepository Events
    {
        get
        {
            _eventRepository ??= new EventRepository(_dbContext);
            return _eventRepository;
        }
    }

    /// <summary>
    /// Gets the repository for Booking entities.
    /// </summary>
    public IBookingRepository Bookings
    {
        get
        {
            _bookingRepository ??= new BookingRepository(_dbContext);
            return _bookingRepository;
        }
    }

    /// <summary>
    /// Gets the repository for Ticket entities.
    /// </summary>
    public ITicketRepository Tickets
    {
        get
        {
            _ticketRepository ??= new TicketRepository(_dbContext);
            return _ticketRepository;
        }
    }

    /// <summary>
    /// Gets the repository for Category entities.
    /// </summary>
    public ICategoryRepository Categories
    {
        get
        {
            _categoryRepository ??= new CategoryRepository(_dbContext);
            return _categoryRepository;
        }
    }

    /// <summary>
    /// Gets the repository for TicketType entities.
    /// </summary>
    public ITicketTypeRepository TicketTypes
    {
        get
        {
            _ticketTypeRepository ??= new TicketTypeRepository(_dbContext);
            return _ticketTypeRepository;
        }
    }

    /// <summary>
    /// Gets the repository for BookingItem entities.
    /// </summary>
    public IBookingItemRepository BookingItems
    {
        get
        {
            _bookingItemRepository ??= new BookingItemRepository(_dbContext);
            return _bookingItemRepository;
        }
    }

    /// <summary>
    /// Gets the repository for Payment entities.
    /// </summary>
    public IPaymentRepository Payments
    {
        get
        {
            _paymentRepository ??= new PaymentRepository(_dbContext);
            return _paymentRepository;
        }
    }

    /// <summary>
    /// Gets the repository for Refund entities.
    /// </summary>
    public IRefundRepository Refunds
    {
        get
        {
            _refundRepository ??= new RefundRepository(_dbContext);
            return _refundRepository;
        }
    }

    /// <summary>
    /// Gets the repository for Review entities.
    /// </summary>
    public IReviewRepository Reviews
    {
        get
        {
            _reviewRepository ??= new ReviewRepository(_dbContext);
            return _reviewRepository;
        }
    }

    /// <summary>
    /// Gets the repository for WaitingList entities.
    /// </summary>
    public IWaitingListRepository WaitingLists
    {
        get
        {
            _waitingListRepository ??= new WaitingListRepository(_dbContext);
            return _waitingListRepository;
        }
    }

    /// <summary>
    /// Gets the repository for ScanLog entities.
    /// </summary>
    public IScanLogRepository ScanLogs
    {
        get
        {
            _scanLogRepository ??= new ScanLogRepository(_dbContext);
            return _scanLogRepository;
        }
    }

    /// <summary>
    /// Gets the repository for OutboxMessage entities.
    /// </summary>
    public IOutboxMessageRepository OutboxMessages
    {
        get
        {
            _outboxMessageRepository ??= new OutboxMessageRepository(_dbContext);
            return _outboxMessageRepository;
        }
    }

    /// <summary>
    /// Gets the repository for SavedEvent entities.
    /// </summary>
    public ISavedEventRepository SavedEvents
    {
        get
        {
            _savedEventRepository ??= new SavedEventRepository(_dbContext);
            return _savedEventRepository;
        }
    }

    /// <summary>
    /// Gets the repository for Notification entities.
    /// </summary>
    public INotificationRepository Notifications
    {
        get
        {
            _notificationRepository ??= new NotificationRepository(_dbContext);
            return _notificationRepository;
        }
    }

    /// <summary>
    /// Gets the repository for ApplicationUser entities.
    /// </summary>
    public IGenericRepository<ApplicationUser> Users
    {
        get
        {
            _userRepository ??= new GenericRepository<ApplicationUser>(_dbContext);
            return _userRepository;
        }
    }

    public IGenericRepository<OrganizerProfile> OrganizerProfiles
    {
        get
        {
            _organizerProfileRepository ??= new GenericRepository<OrganizerProfile>(_dbContext);
            return _organizerProfileRepository;
        }
    }

    /// <summary>
    /// Gets the underlying DbContext instance.
    /// </summary>
    public EventifyDbContext DbContext => _dbContext;

    /// <summary>
    /// Saves all changes made in all repositories to the database.
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Begins a database transaction.
    /// </summary>
    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>
    /// Rolls back all pending changes.
    /// </summary>
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        var currentTransaction = _dbContext.Database.CurrentTransaction;
        if (currentTransaction != null)
        {
            await currentTransaction.RollbackAsync(cancellationToken);
        }
        else
        {
            foreach (var entry in _dbContext.ChangeTracker.Entries())
            {
                switch (entry.State)
                {
                    case EntityState.Modified:
                    case EntityState.Deleted:
                        entry.Reload();
                        break;
                    case EntityState.Added:
                        entry.State = EntityState.Detached;
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Disposes the UnitOfWork and its DbContext.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_dbContext != null)
        {
            await _dbContext.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Synchronously disposes the UnitOfWork and its DbContext.
    /// </summary>
    public void Dispose()
    {
        if (_dbContext != null)
        {
            _dbContext.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}
