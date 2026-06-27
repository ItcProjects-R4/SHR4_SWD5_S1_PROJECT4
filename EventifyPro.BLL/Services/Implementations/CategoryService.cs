namespace EventifyPro.BLL.Services.Implementations;

/// <summary>
/// Service for managing event categories.
/// Provides CRUD operations with comprehensive error handling and performance optimization.
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CategoryService> _logger;
    private readonly IValidator<CategoryCreateDto> _createValidator;
    private readonly IValidator<CategoryUpdateDto> _updateValidator;
    private readonly ICacheInvalidationService _cacheInvalidationService;
    private readonly IMemoryCache _cache;

    public CategoryService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CategoryService> logger,
        IValidator<CategoryCreateDto> createValidator,
        IValidator<CategoryUpdateDto> updateValidator,
        ICacheInvalidationService cacheInvalidationService,
        IMemoryCache cache)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _cacheInvalidationService = cacheInvalidationService;
        _cache = cache;
    }

    /// <summary>
    /// Retrieves all categories ordered by name.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing all categories ordered alphabetically.</returns>
    public async Task<Result<IReadOnlyList<CategoryDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = "AllCategoriesList";
            if (_cache.TryGetValue(cacheKey, out IReadOnlyList<CategoryDto>? cachedCategories) && cachedCategories != null)
            {
                _logger.LogDebug("Retrieving all categories from memory cache");
                return Result<IReadOnlyList<CategoryDto>>.Success(cachedCategories);
            }

            _logger.LogDebug("Retrieving all categories from database");

            // Project directly to CategoryDto using Mapster to compile Count() directly into SQL SELECT for optimal DB performance
            var categoryDtos = await _unitOfWork.Categories.GetQuery()
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ProjectToType<CategoryDto>()
                .ToListAsync(cancellationToken);

            var result = categoryDtos.AsReadOnly();
            
            // Cache categories for 1 hour
            _cache.Set(cacheKey, result, TimeSpan.FromHours(1));

            _logger.LogDebug("Successfully retrieved {Count} categories", categoryDtos.Count);
            return Result<IReadOnlyList<CategoryDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all categories");
            return Result<IReadOnlyList<CategoryDto>>.Failure("Failed to retrieve categories");
        }
    }

    public async Task<Result<CategoryDto>> CreateAsync(CategoryCreateDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Creating new category: {Name}", dto.Name);

            var validationError = await _createValidator.GetValidationErrorAsync(dto, cancellationToken);
            if (validationError is not null)
            {
                _logger.LogWarning("Category creation failed validation: {Error}", validationError);
                return Result<CategoryDto>.Failure(validationError);
            }

            var sanitizedName = dto.Name.Trim();

            // Check for duplicate name (case-insensitive)
            var existingCategory = await _unitOfWork.Categories.ExistsByNameAsync(sanitizedName, cancellationToken);
            if (existingCategory)
            {
                _logger.LogWarning("Category creation failed: duplicate name '{Name}'", sanitizedName);
                return Result<CategoryDto>.Failure($"Category '{sanitizedName}' already exists");
            }

            // Map DTO to entity with sanitized name
            var category = _mapper.Map<Category>(dto);
            category.Name = sanitizedName;
            if (!string.IsNullOrWhiteSpace(dto.Description))
                category.Description = dto.Description.Trim();

            // Add to repository
            await _unitOfWork.Categories.AddAsync(category, cancellationToken);

            // Commit changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _cache.Remove("AllCategoriesList");

            await _cacheInvalidationService.InvalidateEventCacheAsync(cancellationToken);

            _logger.LogInformation("Category '{Name}' created successfully with ID {Id}", sanitizedName, category.Id);

            // Map back to DTO and return
            var categoryDto = _mapper.Map<CategoryDto>(category);
            return Result<CategoryDto>.Success(categoryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category: {Name}", dto.Name);
            await _unitOfWork.RollbackAsync();
            return Result<CategoryDto>.Failure("Failed to create category");
        }
    }

    public async Task<Result<CategoryDto>> UpdateAsync(int id, CategoryUpdateDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Updating category with ID {Id}", id);

            var validationError = await _updateValidator.GetValidationErrorAsync(dto with { Id = id }, cancellationToken);
            if (validationError is not null)
            {
                _logger.LogWarning("Category update failed validation for ID {Id}: {Error}", id, validationError);
                return Result<CategoryDto>.Failure(validationError);
            }

            // Retrieve existing category
            var existingCategory = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);
            if (existingCategory == null)
            {
                _logger.LogWarning("Category update failed: category not found with ID {Id}", id);
                return Result<CategoryDto>.Failure("Category not found");
            }

            // Sanitize input
            var sanitizedName = dto.Name.Trim();

            // Check for duplicate name if name is being changed (case-insensitive)
            if (!existingCategory.Name.Equals(sanitizedName, StringComparison.OrdinalIgnoreCase))
            {
                var duplicateExists = await _unitOfWork.Categories.ExistsByNameAsync(sanitizedName, cancellationToken);
                if (duplicateExists)
                {
                    _logger.LogWarning("Category update failed: duplicate name '{Name}' for ID {Id}", sanitizedName, id);
                    return Result<CategoryDto>.Failure($"Category '{sanitizedName}' already exists");
                }
            }

            // Update entity properties
            existingCategory.Name = sanitizedName;
            if (!string.IsNullOrWhiteSpace(dto.Description))
                existingCategory.Description = dto.Description.Trim();
            else
                existingCategory.Description = null;

            // Update in repository
            _unitOfWork.Categories.Update(existingCategory);

            // Commit changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _cache.Remove("AllCategoriesList");

            await _cacheInvalidationService.InvalidateEventCacheAsync(cancellationToken);

            _logger.LogInformation("Category with ID {Id} updated successfully to name '{Name}'", id, sanitizedName);

            // Map back to DTO and return
            var categoryDto = _mapper.Map<CategoryDto>(existingCategory);
            return Result<CategoryDto>.Success(categoryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category with ID {Id}", id);
            await _unitOfWork.RollbackAsync();
            return Result<CategoryDto>.Failure("Failed to update category");
        }
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting category with ID {Id}", id);

            // Validate input
            if (id <= 0)
            {
                _logger.LogWarning("Category deletion failed: invalid ID {Id}", id);
                return Result.Failure("Invalid category ID");
            }

            // Retrieve existing category
            var existingCategory = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);
            if (existingCategory == null)
            {
                _logger.LogWarning("Category deletion failed: category not found with ID {Id}", id);
                return Result.Failure("Category not found");
            }

            // Check if category is used by any events (FK constraint)
            var isUsedByEvents = await _unitOfWork.Categories.IsUsedByAnyEventAsync(id, cancellationToken);
            if (isUsedByEvents)
            {
                _logger.LogWarning("Category deletion failed: category ID {Id} is referenced by events", id);
                return Result.Failure("Cannot delete category that is associated with events. Remove or reassign events first.");
            }

            // Delete from repository
            _unitOfWork.Categories.Delete(existingCategory);

            // Commit changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _cache.Remove("AllCategoriesList");

            await _cacheInvalidationService.InvalidateEventCacheAsync(cancellationToken);

            _logger.LogInformation("Category with ID {Id} deleted successfully", id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category with ID {Id}", id);
            await _unitOfWork.RollbackAsync();
            return Result.Failure("Failed to delete category");
        }
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Categories.GetQuery().AsNoTracking();
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }
        var trimmedName = name.Trim();
        return await query.AnyAsync(c => c.Name.ToLower() == trimmedName.ToLower(), cancellationToken);
    }
}

