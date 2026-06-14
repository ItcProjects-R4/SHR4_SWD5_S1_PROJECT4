namespace EventifyPro.BLL.Mappings;

#pragma warning disable CS8603 // Mapster Ignore expressions can target nullable members safely.

public sealed class UserMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ApplicationUser, UserProfileDto>();

        config.NewConfig<UserUpdateProfileDto, ApplicationUser>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.UserName)
            .Ignore(dest => dest.NormalizedUserName)
            .Ignore(dest => dest.Email)
            .Ignore(dest => dest.NormalizedEmail)
            .Ignore(dest => dest.EmailConfirmed)
            .Ignore(dest => dest.PasswordHash)
            .Ignore(dest => dest.SecurityStamp)
            .Ignore(dest => dest.ConcurrencyStamp)
            .Ignore(dest => dest.PhoneNumber)
            .Ignore(dest => dest.PhoneNumberConfirmed)
            .Ignore(dest => dest.TwoFactorEnabled)
            .Ignore(dest => dest.LockoutEnd)
            .Ignore(dest => dest.LockoutEnabled)
            .Ignore(dest => dest.AccessFailedCount)
            .Ignore(dest => dest.IsActive)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.ScannerCreatedByOrganizerId)
            .Ignore(dest => dest.ScannerCreatedByOrganizer)
            .Ignore(dest => dest.CreatedScannerAccounts)
            .Ignore(dest => dest.OrganizedEvents)
            .Ignore(dest => dest.Bookings)
            .Ignore(dest => dest.Reviews)
            .Ignore(dest => dest.InitiatedRefunds)
            .Ignore(dest => dest.WaitingListItems)
            .Ignore(dest => dest.ScanLogs)
            .Ignore(dest => dest.ScannedTickets)
            .Map(dest => dest.UpdatedAt, _ => DateTime.UtcNow);
    }
}

#pragma warning restore CS8603
