namespace EventifyPro.BLL.Services.Implementations
{
    public class AdminService : IAdminService
    {
        private readonly EventifyDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IOutboxService _outboxService;
        private readonly IMemoryCache _cache;

        public AdminService(
            EventifyDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOutboxService outboxService,
            IMemoryCache cache)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _outboxService = outboxService;
            _cache = cache;
        }

        public async Task<Result<AdminDashboardDataDto>> GetDashboardDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var pendingFeedback = await _context.Feedback
                    .AsNoTracking()
                    .Where(f => !f.IsApproved)
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync(cancellationToken);

                var approvedFeedback = await _context.Feedback
                    .AsNoTracking()
                    .Where(f => f.IsApproved)
                    .OrderByDescending(f => f.CreatedAt)
                    .Take(8)
                    .ToListAsync(cancellationToken);

                var pendingOrganizers = await _context.OrganizerProfiles
                    .Include(p => p.User)
                    .AsNoTracking()
                    .Where(p => !p.IsVerified && (p.RejectionReason == null || p.RejectionReason == ""))
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync(cancellationToken);

                var verifiedOrganizers = await _context.OrganizerProfiles
                    .Include(p => p.User)
                    .AsNoTracking()
                    .Where(p => p.IsVerified)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync(cancellationToken);

                var pendingEvents = await _context.Events
                    .Include(e => e.Category)
                    .Include(e => e.Organizer)
                    .Include(e => e.TicketTypes)
                    .AsNoTracking()
                    .Where(e => e.Status == EventStatus.PendingReview && !e.IsDeleted)
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync(cancellationToken);

                var dto = new AdminDashboardDataDto
                {
                    PendingFeedback = pendingFeedback,
                    ApprovedFeedback = approvedFeedback,
                    PendingOrganizers = pendingOrganizers,
                    VerifiedOrganizers = verifiedOrganizers,
                    PendingEvents = pendingEvents
                };

                return Result<AdminDashboardDataDto>.Success(dto);
            }
            catch (Exception)
            {
                return Result<AdminDashboardDataDto>.Failure("An error occurred while loading dashboard data.");
            }
        }

        public async Task<Result<bool>> ApproveFeedbackAsync(int id, string adminId, CancellationToken cancellationToken = default)
        {
            try
            {
                var feedback = await _context.Feedback.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
                if (feedback is null)
                {
                    return Result<bool>.Failure("Feedback not found.");
                }

                feedback.IsApproved = true;
                feedback.ApprovedAt = DateTime.UtcNow;
                feedback.ApprovedById = adminId;
                await _context.SaveChangesAsync(cancellationToken);

                _cache.Remove("GuestLandingPageData");

                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure("An error occurred while approving feedback.");
            }
        }

        public async Task<Result<bool>> DeleteFeedbackAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var feedback = await _context.Feedback.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
                if (feedback is null)
                {
                    return Result<bool>.Failure("Feedback not found.");
                }

                _context.Feedback.Remove(feedback);
                await _context.SaveChangesAsync(cancellationToken);

                _cache.Remove("GuestLandingPageData");

                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure("An error occurred while deleting feedback.");
            }
        }

        public async Task<Result<bool>> ApproveOrganizerAsync(string userId, string adminId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return Result<bool>.Failure("User ID is required.");
                }

                var profile = await _context.OrganizerProfiles
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

                if (profile is null)
                {
                    return Result<bool>.Failure("Organizer profile not found.");
                }

                profile.IsVerified = true;
                profile.VerifiedAt = DateTime.UtcNow;
                profile.VerifiedById = adminId;
                profile.UpdatedAt = DateTime.UtcNow;

                _context.OrganizerProfiles.Update(profile);
                await _context.SaveChangesAsync(cancellationToken);

                await _outboxService.EnqueueAsync(
                    "Email.OrganizerActivated",
                    new OutboxService.WelcomePayload
                    {
                        RecipientEmail = profile.User.Email!,
                        RecipientName = profile.User.FullName
                    },
                    DateTime.UtcNow.AddSeconds(5),
                    cancellationToken);

                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure("An error occurred while approving organizer.");
            }
        }

        public async Task<Result<bool>> RejectOrganizerAsync(string userId, string reason, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return Result<bool>.Failure("User ID is required.");
                }

                var profile = await _context.OrganizerProfiles
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

                if (profile is null)
                {
                    return Result<bool>.Failure("Organizer profile not found.");
                }

                var user = profile.User;

                profile.RejectionReason = reason;
                profile.IsVerified = false;
                profile.UpdatedAt = DateTime.UtcNow;
                _context.OrganizerProfiles.Update(profile);
                await _context.SaveChangesAsync(cancellationToken);

                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, RoleNames.Attendee);

                await _outboxService.EnqueueAsync(
                    "Email.OrganizerRejected",
                    new
                    {
                        RecipientEmail = user.Email!,
                        RecipientName = user.FullName,
                        RejectionReason = reason
                    },
                    DateTime.UtcNow.AddSeconds(5),
                    cancellationToken);

                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure("An error occurred while rejecting organizer.");
            }
        }

        public async Task<Result<AdminUsersPageDto>> GetUsersPageAsync(string? searchTerm, string? roleFilter, int? page, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.Users.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(u => u.FullName.Contains(searchTerm) || (u.Email != null && u.Email.Contains(searchTerm)));
                }

                if (!string.IsNullOrWhiteSpace(roleFilter))
                {
                    query = from u in query
                            join ur in _context.UserRoles on u.Id equals ur.UserId
                            join r in _context.Roles on ur.RoleId equals r.Id
                            where r.Name == roleFilter
                            select u;
                }

                var totalCount = await query.CountAsync(cancellationToken);
                var pageNumber = page ?? 1;
                var pageSize = 10;
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var users = await query
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                var userIds = users.Select(u => u.Id).ToList();
                var userRoles = await (from ur in _context.UserRoles
                                       join r in _context.Roles on ur.RoleId equals r.Id
                                       where userIds.Contains(ur.UserId)
                                       select new { ur.UserId, RoleName = r.Name })
                                      .ToListAsync(cancellationToken);

                var userRolesDict = userRoles
                    .GroupBy(ur => ur.UserId)
                    .ToDictionary(g => g.Key, g => g.First().RoleName ?? "Attendee");

                var totalUsersCount = await _context.Users.CountAsync(cancellationToken);
                var activeUsersCount = await _context.Users.CountAsync(u => u.IsActive, cancellationToken);
                var inactiveUsersCount = totalUsersCount - activeUsersCount;

                var organizerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == RoleNames.Organizer, cancellationToken);
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == RoleNames.Admin, cancellationToken);
                var scannerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == RoleNames.Scanner, cancellationToken);

                var organizersCount = organizerRole != null ? await _context.UserRoles.CountAsync(ur => ur.RoleId == organizerRole.Id, cancellationToken) : 0;
                var adminsCount = adminRole != null ? await _context.UserRoles.CountAsync(ur => ur.RoleId == adminRole.Id, cancellationToken) : 0;
                var scannersCount = scannerRole != null ? await _context.UserRoles.CountAsync(ur => ur.RoleId == scannerRole.Id, cancellationToken) : 0;
                var attendeesCount = totalUsersCount - (organizersCount + adminsCount + scannersCount);

                var dto = new AdminUsersPageDto
                {
                    Users = users,
                    UserRoles = userRolesDict,
                    TotalCount = totalCount,
                    TotalUsersCount = totalUsersCount,
                    ActiveUsersCount = activeUsersCount,
                    InactiveUsersCount = inactiveUsersCount,
                    OrganizersCount = organizersCount,
                    AdminsCount = adminsCount,
                    ScannersCount = scannersCount,
                    AttendeesCount = attendeesCount,
                    TotalPages = totalPages
                };

                return Result<AdminUsersPageDto>.Success(dto);
            }
            catch (Exception)
            {
                return Result<AdminUsersPageDto>.Failure("An error occurred while loading users.");
            }
        }

        public async Task<Result<bool>> ManageUserRoleAsync(string userId, string newRole, bool isActive, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Result<bool>.Failure("User not found.");
                }

                user.IsActive = isActive;
                user.UpdatedAt = DateTime.UtcNow;

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    return Result<bool>.Failure("Failed to update user status.");
                }

                // Invalidate user session immediately
                await _userManager.UpdateSecurityStampAsync(user);

                var currentRoles = await _userManager.GetRolesAsync(user);
                if (!currentRoles.Contains(newRole))
                {
                    var roleExists = await _roleManager.RoleExistsAsync(newRole);
                    if (!roleExists)
                    {
                        return Result<bool>.Failure($"Role '{newRole}' does not exist.");
                    }

                    var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (!removeResult.Succeeded)
                    {
                        return Result<bool>.Failure("Failed to remove current roles.");
                    }

                    var addResult = await _userManager.AddToRoleAsync(user, newRole);
                    if (!addResult.Succeeded)
                    {
                        return Result<bool>.Failure("Failed to assign new role.");
                    }
                }

                if (newRole == RoleNames.Organizer)
                {
                    var existingProfile = await _context.OrganizerProfiles
                        .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
                    if (existingProfile == null)
                    {
                        var profile = new OrganizerProfile
                        {
                            UserId = userId,
                            OrganizationName = user.FullName ?? "Unnamed Organizer",
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.OrganizerProfiles.Add(profile);
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                }

                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure("An error occurred while managing user role.");
            }
        }

        public async Task<Result<bool>> UnlockUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Result<bool>.Failure("User not found.");
                }

                var result = await _userManager.SetLockoutEndDateAsync(user, null);
                if (!result.Succeeded)
                {
                    return Result<bool>.Failure("Failed to unlock user account.");
                }

                await _userManager.ResetAccessFailedCountAsync(user);

                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure("An error occurred while unlocking user.");
            }
        }

        public async Task<Result<IReadOnlyList<Category>>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var categories = await _context.Categories
                    .AsNoTracking()
                    .OrderBy(c => c.Name)
                    .ToListAsync(cancellationToken);

                return Result<IReadOnlyList<Category>>.Success(categories);
            }
            catch (Exception)
            {
                return Result<IReadOnlyList<Category>>.Failure("An error occurred while retrieving categories.");
            }
        }

        public async Task<Result<bool>> CreateCategoryAsync(string name, string description, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Result<bool>.Failure("Category name is required.");
                }

                var exists = await _context.Categories
                    .AnyAsync(c => c.Name.ToLower() == name.Trim().ToLower(), cancellationToken);
                if (exists)
                {
                    return Result<bool>.Failure("A category with this name already exists.");
                }

                var category = new Category
                {
                    Name = name.Trim(),
                    Description = description?.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync(cancellationToken);

                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure("An error occurred while creating category.");
            }
        }

        public async Task<Result<bool>> UpdateCategoryAsync(int categoryId, string name, string description, CancellationToken cancellationToken = default)
        {
            try
            {
                var category = await _context.Categories.FindAsync(new object[] { categoryId }, cancellationToken: cancellationToken);
                if (category == null)
                {
                    return Result<bool>.Failure("Category not found.");
                }

                var exists = await _context.Categories
                    .AnyAsync(c => c.Id != categoryId && c.Name.ToLower() == name.Trim().ToLower(), cancellationToken);
                if (exists)
                {
                    return Result<bool>.Failure("A category with this name already exists.");
                }

                category.Name = name.Trim();
                category.Description = description?.Trim();
                category.UpdatedAt = DateTime.UtcNow;

                _context.Categories.Update(category);
                await _context.SaveChangesAsync(cancellationToken);

                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure("An error occurred while updating category.");
            }
        }

        public async Task<Result<bool>> DeleteCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
        {
            try
            {
                var category = await _context.Categories.FindAsync(new object[] { categoryId }, cancellationToken: cancellationToken);
                if (category == null)
                {
                    return Result<bool>.Failure("Category not found.");
                }

                var hasAssociatedEvents = await _context.Events.AnyAsync(e => e.CategoryId == categoryId, cancellationToken);
                if (hasAssociatedEvents)
                {
                    return Result<bool>.Failure("Cannot delete category. It is associated with one or more events.");
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync(cancellationToken);

                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure("An error occurred while deleting category.");
            }
        }

        public async Task<Result<PagedResult<Event>>> GetEventsPageAsync(string? searchTerm, string? statusFilter, int? page, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.Events.Include(e => e.Category).AsNoTracking();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(e => e.Title.Contains(searchTerm) || e.Description.Contains(searchTerm));
                }

                if (!string.IsNullOrWhiteSpace(statusFilter))
                {
                    query = statusFilter.ToLower() switch
                    {
                        "pending" => query.Where(e => e.Status == EventStatus.PendingReview),
                        "published" => query.Where(e => e.Status == EventStatus.Published),
                        "completed" => query.Where(e => e.Status == EventStatus.Completed),
                        "cancelled" => query.Where(e => e.Status == EventStatus.Cancelled),
                        _ => query
                    };
                }

                var totalCount = await query.CountAsync(cancellationToken);
                var pageNumber = page ?? 1;
                var pageSize = 10;

                var events = await query
                    .OrderByDescending(e => e.StartDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                var pagedResult = PagedResult<Event>.Create(events, totalCount, pageNumber, pageSize);
                return Result<PagedResult<Event>>.Success(pagedResult);
            }
            catch (Exception)
            {
                return Result<PagedResult<Event>>.Failure("An error occurred while loading events.");
            }
        }

        public async Task<Result<PagedResult<Review>>> GetReviewsPageAsync(string? searchTerm, int? ratingFilter, bool? hiddenFilter, int? page, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.Reviews
                    .IgnoreQueryFilters()
                    .Include(r => r.Event)
                    .Include(r => r.User)
                    .AsNoTracking();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(r => (r.Comment ?? "").Contains(searchTerm) || r.Event.Title.Contains(searchTerm));
                }

                if (ratingFilter.HasValue)
                {
                    query = query.Where(r => r.Rating == ratingFilter);
                }

                if (hiddenFilter.HasValue)
                {
                    query = query.Where(r => r.IsHidden == hiddenFilter);
                }

                var totalCount = await query.CountAsync(cancellationToken);
                var pageNumber = page ?? 1;
                var pageSize = 10;

                var reviews = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                var pagedResult = PagedResult<Review>.Create(reviews, totalCount, pageNumber, pageSize);
                return Result<PagedResult<Review>>.Success(pagedResult);
            }
            catch (Exception)
            {
                return Result<PagedResult<Review>>.Failure("An error occurred while loading reviews.");
            }
        }

        public async Task<Result<bool>> ApproveReviewAsync(int reviewId, string adminId, CancellationToken cancellationToken = default)
        {
            try
            {
                var review = await _context.Reviews.FindAsync(new object[] { reviewId }, cancellationToken: cancellationToken);
                if (review is null)
                {
                    return Result<bool>.Failure("Review not found.");
                }

                review.IsHidden = false;
                review.HiddenById = null;
                review.HiddenReason = null;
                review.UpdatedAt = DateTime.UtcNow;

                _context.Reviews.Update(review);
                await _context.SaveChangesAsync(cancellationToken);

                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure("An error occurred while approving review.");
            }
        }

        public async Task<Result<bool>> FlagReviewAsync(int reviewId, string adminId, string? reason = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var review = await _context.Reviews.FindAsync(new object[] { reviewId }, cancellationToken: cancellationToken);
                if (review is null)
                {
                    return Result<bool>.Failure("Review not found.");
                }

                review.IsHidden = true;
                review.HiddenById = adminId;
                review.HiddenReason = string.IsNullOrWhiteSpace(reason) ? "Flagged by Admin" : reason.Trim();
                review.UpdatedAt = DateTime.UtcNow;

                _context.Reviews.Update(review);
                await _context.SaveChangesAsync(cancellationToken);

                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure("An error occurred while flagging review.");
            }
        }

        public async Task<Result<bool>> DeleteReviewAsync(int reviewId, CancellationToken cancellationToken = default)
        {
            try
            {
                var review = await _context.Reviews.FindAsync(new object[] { reviewId }, cancellationToken: cancellationToken);
                if (review is null)
                {
                    return Result<bool>.Failure("Review not found.");
                }

                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync(cancellationToken);

                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure("An error occurred while deleting review.");
            }
        }

        public async Task<Result<AdminAuditLogsDto>> GetAuditLogsPageAsync(string? tableNameFilter, string? actionFilter, string? userIdFilter, DateTime? startDate, DateTime? endDate, int? page, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.AuditLogs.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(tableNameFilter))
                {
                    query = query.Where(a => a.TableName == tableNameFilter);
                }

                if (!string.IsNullOrWhiteSpace(actionFilter))
                {
                    query = query.Where(a => a.Action == actionFilter);
                }

                if (!string.IsNullOrWhiteSpace(userIdFilter))
                {
                    query = query.Where(a => a.UserId == userIdFilter);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(a => a.ChangedAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(a => a.ChangedAt <= endDate.Value);
                }

                var totalCount = await query.CountAsync(cancellationToken);
                var pageNumber = page ?? 1;
                var pageSize = 15;

                var logs = await query
                    .OrderByDescending(a => a.ChangedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                var uniqueTables = await _cache.GetOrCreateAsync("AuditLog_UniqueTables", async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    return await _context.AuditLogs
                        .Select(a => a.TableName)
                        .Distinct()
                        .OrderBy(t => t)
                        .ToListAsync(cancellationToken);
                }) ?? new List<string>();

                var uniqueActions = await _cache.GetOrCreateAsync("AuditLog_UniqueActions", async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    return await _context.AuditLogs
                        .Select(a => a.Action)
                        .Distinct()
                        .OrderBy(ac => ac)
                        .ToListAsync(cancellationToken);
                }) ?? new List<string>();

                var userIds = logs
                    .Select(l => l.UserId)
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Distinct()
                    .ToList();

                var userMap = await _context.Users
                    .Where(u => userIds.Contains(u.Id))
                    .Select(u => new { u.Id, Display = u.FullName ?? u.Email ?? "Unknown" })
                    .ToDictionaryAsync(u => u.Id, u => u.Display, cancellationToken);

                var pagedLogs = PagedResult<AuditLog>.Create(logs, totalCount, pageNumber, pageSize);

                var dto = new AdminAuditLogsDto
                {
                    PagedLogs = pagedLogs,
                    UniqueTables = uniqueTables,
                    UniqueActions = uniqueActions,
                    UserMap = userMap
                };

                return Result<AdminAuditLogsDto>.Success(dto);
            }
            catch (Exception)
            {
                return Result<AdminAuditLogsDto>.Failure("An error occurred while loading audit logs.");
            }
        }

        public async Task<Result<AdminUserDetailsDto>> GetUserDetailsAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Result<AdminUserDetailsDto>.Failure("User ID is required.");
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user is null)
                {
                    return Result<AdminUserDetailsDto>.Failure("User not found.");
                }

                var roles = await _userManager.GetRolesAsync(user);
                var primaryRole = roles.FirstOrDefault() ?? "Attendee";

                var bookings = await _context.Bookings
                    .Include(b => b.Event)
                    .AsNoTracking()
                    .Where(b => b.UserId == userId)
                    .OrderByDescending(b => b.BookingDate)
                    .ToListAsync(cancellationToken);

                var payments = await _context.Payments
                    .Include(p => p.Booking)
                        .ThenInclude(b => b.Event)
                    .AsNoTracking()
                    .Where(p => p.Booking.UserId == userId)
                    .OrderByDescending(p => p.PaymentDate)
                    .ToListAsync(cancellationToken);

                var reviews = await _context.Reviews
                    .IgnoreQueryFilters()
                    .Include(r => r.Event)
                    .AsNoTracking()
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync(cancellationToken);

                var auditLogs = await _context.AuditLogs
                    .AsNoTracking()
                    .Where(l => l.UserId == userId)
                    .OrderByDescending(l => l.ChangedAt)
                    .Take(50)
                    .ToListAsync(cancellationToken);

                var dto = new AdminUserDetailsDto
                {
                    User = user,
                    PrimaryRole = primaryRole,
                    Bookings = bookings,
                    Payments = payments,
                    Reviews = reviews,
                    AuditLogs = auditLogs
                };

                return Result<AdminUserDetailsDto>.Success(dto);
            }
            catch (Exception)
            {
                return Result<AdminUserDetailsDto>.Failure("An error occurred while retrieving user details.");
            }
        }

        public async Task<Result<bool>> BulkApproveReviewsAsync(IEnumerable<int> reviewIds, string adminId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (reviewIds == null || !reviewIds.Any())
                {
                    return Result<bool>.Failure("No reviews specified for approval.");
                }

                var reviews = await _context.Reviews
                    .IgnoreQueryFilters()
                    .Where(r => reviewIds.Contains(r.Id))
                    .ToListAsync(cancellationToken);

                foreach (var review in reviews)
                {
                    review.IsHidden = false;
                    review.HiddenById = null;
                    review.HiddenReason = null;
                    review.UpdatedAt = DateTime.UtcNow;
                }

                _context.Reviews.UpdateRange(reviews);
                await _context.SaveChangesAsync(cancellationToken);

                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure("An error occurred while bulk approving reviews.");
            }
        }

        public async Task<Result<bool>> BulkDeleteReviewsAsync(IEnumerable<int> reviewIds, CancellationToken cancellationToken = default)
        {
            try
            {
                if (reviewIds == null || !reviewIds.Any())
                {
                    return Result<bool>.Failure("No reviews specified for deletion.");
                }

                var reviews = await _context.Reviews
                    .IgnoreQueryFilters()
                    .Where(r => reviewIds.Contains(r.Id))
                    .ToListAsync(cancellationToken);

                _context.Reviews.RemoveRange(reviews);
                await _context.SaveChangesAsync(cancellationToken);

                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure("An error occurred while bulk deleting reviews.");
            }
        }

        public async Task<Result<bool>> BulkApproveOrganizersAsync(IEnumerable<string> userIds, string adminId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (userIds == null || !userIds.Any())
                {
                    return Result<bool>.Failure("No organizers specified for approval.");
                }

                var profiles = await _context.OrganizerProfiles
                    .Include(p => p.User)
                    .Where(p => userIds.Contains(p.UserId))
                    .ToListAsync(cancellationToken);

                foreach (var profile in profiles)
                {
                    profile.IsVerified = true;
                    profile.VerifiedAt = DateTime.UtcNow;
                    profile.VerifiedById = adminId;
                    profile.UpdatedAt = DateTime.UtcNow;

                    _context.OrganizerProfiles.Update(profile);

                    await _outboxService.EnqueueAsync(
                        "Email.OrganizerActivated",
                        new OutboxService.WelcomePayload
                        {
                            RecipientEmail = profile.User.Email!,
                            RecipientName = profile.User.FullName
                        },
                        DateTime.UtcNow.AddSeconds(5));
                }

                await _context.SaveChangesAsync(cancellationToken);
                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure("An error occurred while bulk approving organizers.");
            }
        }

        public async Task<Result<IReadOnlyList<AdminSentNotificationDto>>> GetSentNotificationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var sentNotifications = await _context.Notifications
                    .AsNoTracking()
                    .Where(n => n.Type == NotificationType.Maintenance || n.Type == NotificationType.SystemUpdate || n.Type == NotificationType.CustomAlert)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync(cancellationToken);

                var grouped = sentNotifications
                    .GroupBy(n => new { n.Title, n.Message, n.Type, n.CreatedAt })
                    .ToList();

                var dtoList = new List<AdminSentNotificationDto>();

                foreach (var g in grouped)
                {
                    string recipient = "System-Wide (All Users)";
                    if (g.Count() == 1)
                    {
                        var firstNotification = g.First();
                        if (!string.IsNullOrEmpty(firstNotification.UserId))
                        {
                            var user = await _userManager.FindByIdAsync(firstNotification.UserId);
                            recipient = user?.Email ?? "Unknown User";
                        }
                    }

                    dtoList.Add(new AdminSentNotificationDto
                    {
                        Title = g.Key.Title,
                        Message = g.Key.Message,
                        Type = g.Key.Type,
                        CreatedAt = g.Key.CreatedAt,
                        RecipientCount = g.Count(),
                        Recipient = recipient
                    });
                }

                var orderedList = dtoList.OrderByDescending(x => x.CreatedAt).Take(15).ToList();
                return Result<IReadOnlyList<AdminSentNotificationDto>>.Success(orderedList);
            }
            catch (Exception)
            {
                return Result<IReadOnlyList<AdminSentNotificationDto>>.Failure("An error occurred while retrieving sent notifications.");
            }
        }

        public async Task<Result<IReadOnlyList<PayoutRequest>>> GetPayoutRequestsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var payouts = await _context.PayoutRequests
                    .Include(p => p.Organizer)
                    .OrderByDescending(p => p.RequestedAt)
                    .ToListAsync(cancellationToken);

                return Result<IReadOnlyList<PayoutRequest>>.Success(payouts);
            }
            catch (Exception ex)
            {
                return Result<IReadOnlyList<PayoutRequest>>.Failure($"An error occurred while retrieving payout requests: {ex.Message}");
            }
        }

        public async Task<Result<bool>> UpdatePayoutRequestStatusAsync(
            int requestId, 
            string status, 
            string? referenceNumber, 
            string? notes, 
            string adminId, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var payout = await _context.PayoutRequests
                    .Include(p => p.Organizer)
                    .FirstOrDefaultAsync(p => p.Id == requestId, cancellationToken);

                if (payout == null)
                {
                    return Result<bool>.Failure("Payout request not found.");
                }

                if (payout.Status != "Pending")
                {
                    return Result<bool>.Failure("Only pending payout requests can be updated.");
                }

                payout.Status = status;
                payout.ReferenceNumber = referenceNumber;
                payout.Notes = notes;
                payout.ProcessedAt = DateTime.UtcNow;
                payout.UpdatedAt = DateTime.UtcNow;

                _context.PayoutRequests.Update(payout);
                await _context.SaveChangesAsync(cancellationToken);

                // Enqueue email notification to Organizer
                await _outboxService.EnqueueAsync("Email.PayoutStatus", new OutboxService.PayoutStatusPayload
                {
                    RecipientEmail = payout.Organizer.Email!,
                    RecipientName = payout.Organizer.FullName,
                    Amount = payout.Amount,
                    Status = payout.Status,
                    ReferenceNumber = payout.ReferenceNumber,
                    Notes = payout.Notes
                }, cancellationToken);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"An error occurred while updating payout request: {ex.Message}");
            }
        }
    }
}
