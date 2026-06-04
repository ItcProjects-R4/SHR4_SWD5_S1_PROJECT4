using MapsterMapper;

namespace EventifyPro.BLL.Services.Implementations;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CategoryService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public Task<Result<IReadOnlyList<CategoryDto>>> GetAllAsync(CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result<CategoryDto>> CreateAsync(CategoryCreateDto dto, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result<CategoryDto>> UpdateAsync(int id, CategoryUpdateDto dto, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();
}
