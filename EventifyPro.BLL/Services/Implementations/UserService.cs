using MapsterMapper;

namespace EventifyPro.BLL.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IUploadHelper _uploadHelper;

    public UserService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IUploadHelper uploadHelper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _userManager = userManager;
        _roleManager = roleManager;
        _uploadHelper = uploadHelper;
    }

    public Task<Result<UserProfileDto>> GetProfileAsync(string userId, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result<UserProfileDto>> UpdateProfileAsync(string userId, UserUpdateProfileDto dto, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result<IReadOnlyList<UserProfileDto>>> GetAllAsync(CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result> SetActiveAsync(string id, bool isActive, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result> AssignRoleAsync(string id, string roleName, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();
}
