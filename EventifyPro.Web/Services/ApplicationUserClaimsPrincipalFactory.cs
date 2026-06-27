namespace EventifyPro.Web.Services
{
    public class ApplicationUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
    {
        private readonly EventifyDbContext _context;

        public ApplicationUserClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<IdentityOptions> optionsAccessor,
            EventifyDbContext context)
            : base(userManager, roleManager, optionsAccessor)
        {
            _context = context;
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);

            // Cache FullName to avoid database lookups in layout
            identity.AddClaim(new Claim("FullName", user.FullName ?? string.Empty));
            identity.AddClaim(new Claim("ProfileImageUrl", user.ProfileImageUrl ?? string.Empty));

            // If the user is an Organizer, fetch their verification status and cache it in a claim
            var isOrganizer = await UserManager.IsInRoleAsync(user, RoleNames.Organizer);
            if (isOrganizer)
            {
                var isVerified = await _context.OrganizerProfiles
                    .AsNoTracking()
                    .AnyAsync(p => p.UserId == user.Id && p.IsVerified);
                identity.AddClaim(new Claim("IsVerifiedOrganizer", isVerified.ToString().ToLower()));
            }

            return identity;
        }
    }
}
