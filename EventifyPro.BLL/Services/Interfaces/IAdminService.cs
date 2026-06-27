using Eventify.Domain.Entities;
using Eventify.Shared.Wrappers;
using EventifyPro.BLL.DTOs.Admin;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EventifyPro.BLL.Services.Interfaces
{
    public interface IAdminService
    {
        // Index / Dashboard
        Task<Result<AdminDashboardDataDto>> GetDashboardDataAsync(CancellationToken cancellationToken = default);

        // Feedback management
        Task<Result<bool>> ApproveFeedbackAsync(int id, string adminId, CancellationToken cancellationToken = default);
        Task<Result<bool>> DeleteFeedbackAsync(int id, CancellationToken cancellationToken = default);

        // Organizer moderation
        Task<Result<bool>> ApproveOrganizerAsync(string userId, string adminId, CancellationToken cancellationToken = default);
        Task<Result<bool>> RejectOrganizerAsync(string userId, string reason, CancellationToken cancellationToken = default);

        // Users
        Task<Result<AdminUsersPageDto>> GetUsersPageAsync(string? searchTerm, string? roleFilter, int? page, CancellationToken cancellationToken = default);
        Task<Result<bool>> ManageUserRoleAsync(string userId, string newRole, bool isActive, CancellationToken cancellationToken = default);
        Task<Result<bool>> UnlockUserAsync(string userId, CancellationToken cancellationToken = default);

        // Categories
        Task<Result<IReadOnlyList<Category>>> GetCategoriesAsync(CancellationToken cancellationToken = default);
        Task<Result<bool>> CreateCategoryAsync(string name, string description, CancellationToken cancellationToken = default);
        Task<Result<bool>> UpdateCategoryAsync(int categoryId, string name, string description, CancellationToken cancellationToken = default);
        Task<Result<bool>> DeleteCategoryAsync(int categoryId, CancellationToken cancellationToken = default);

        // Events
        Task<Result<PagedResult<Event>>> GetEventsPageAsync(string? searchTerm, string? statusFilter, int? page, CancellationToken cancellationToken = default);

        // Reviews
        Task<Result<PagedResult<Review>>> GetReviewsPageAsync(string? searchTerm, int? ratingFilter, bool? hiddenFilter, int? page, CancellationToken cancellationToken = default);
        Task<Result<bool>> ApproveReviewAsync(int reviewId, string adminId, CancellationToken cancellationToken = default);
        Task<Result<bool>> FlagReviewAsync(int reviewId, string adminId, string? reason = null, CancellationToken cancellationToken = default);
        Task<Result<bool>> DeleteReviewAsync(int reviewId, CancellationToken cancellationToken = default);

        // Audit Logs
        Task<Result<AdminAuditLogsDto>> GetAuditLogsPageAsync(string? tableNameFilter, string? actionFilter, string? userIdFilter, DateTime? startDate, DateTime? endDate, int? page, CancellationToken cancellationToken = default);

        // User Details & Bulk Moderation
        Task<Result<AdminUserDetailsDto>> GetUserDetailsAsync(string userId, CancellationToken cancellationToken = default);
        Task<Result<bool>> BulkApproveReviewsAsync(IEnumerable<int> reviewIds, string adminId, CancellationToken cancellationToken = default);
        Task<Result<bool>> BulkDeleteReviewsAsync(IEnumerable<int> reviewIds, CancellationToken cancellationToken = default);
        Task<Result<bool>> BulkApproveOrganizersAsync(IEnumerable<string> userIds, string adminId, CancellationToken cancellationToken = default);

        // Notifications
        Task<Result<IReadOnlyList<AdminSentNotificationDto>>> GetSentNotificationsAsync(CancellationToken cancellationToken = default);

        // Payout Requests
        Task<Result<IReadOnlyList<PayoutRequest>>> GetPayoutRequestsAsync(CancellationToken cancellationToken = default);
        Task<Result<bool>> UpdatePayoutRequestStatusAsync(int requestId, string status, string? referenceNumber, string? notes, string adminId, CancellationToken cancellationToken = default);
    }
}
