namespace EventifyPro.BLL.Extensions;

public static class ValidatorExtensions
{
    public static string? GetValidationError(this FluentValidation.Results.ValidationResult validationResult)
    {
        if (validationResult.IsValid)
            return null;

        return string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
    }

    public static async Task<string?> GetValidationErrorAsync<T>(
        this IValidator<T> validator,
        T instance,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(instance, cancellationToken);
        return validationResult.GetValidationError();
    }
}
