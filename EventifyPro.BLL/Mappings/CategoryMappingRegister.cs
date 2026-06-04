using EventifyPro.BLL.DTOs.Category;
using Mapster;

namespace EventifyPro.BLL.Mappings;

#pragma warning disable CS8603 // Mapster Ignore expressions can target nullable members safely.

public sealed class CategoryMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Category, CategoryDto>();

        config.NewConfig<CategoryCreateDto, Category>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.Events)
            .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow);

        config.NewConfig<CategoryUpdateDto, Category>()
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.Events)
            .Map(dest => dest.UpdatedAt, _ => DateTime.UtcNow);
    }
}

#pragma warning restore CS8603
