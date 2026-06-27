namespace Eventify.Domain.Entities;

/// <summary>
/// Represents feedback submitted from the website footer.
/// </summary>
public class Feedback
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Email { get; set; }

    public string Message { get; set; } = string.Empty;

    public bool IsApproved { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? ApprovedById { get; set; }
    public ApplicationUser? ApprovedBy { get; set; }
}
