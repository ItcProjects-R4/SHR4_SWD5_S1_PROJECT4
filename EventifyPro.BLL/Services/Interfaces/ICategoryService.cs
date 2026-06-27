namespace EventifyPro.BLL.Services.Interfaces;

public interface ICategoryService
{
    Task<Result<IReadOnlyList<CategoryDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<CategoryDto>> CreateAsync(CategoryCreateDto dto, CancellationToken cancellationToken = default);
    Task<Result<CategoryDto>> UpdateAsync(int id, CategoryUpdateDto dto, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
}
