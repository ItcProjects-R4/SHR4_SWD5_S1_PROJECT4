namespace Eventify.Shared.Wrappers;

public class Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public bool IsFailure => !IsSuccess;

    private Result(bool isSuccess, T? data, string? error)
    {
        IsSuccess = isSuccess;
        Data = data;
        Error = error;
    }

    public static Result<T> Success(T data)
        => new(true, data, null);

    public static Result<T> Failure(string error)
        => new(false, default, error);

    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
    {
        if (IsFailure)
            return Result<TOut>.Failure(Error!);

        return Result<TOut>.Success(mapper(Data!));
    }

    public override string ToString()
        => IsSuccess ? $"Success: {Data}" : $"Failure: {Error}";
}

public class Result
{
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }
    public bool IsFailure => !IsSuccess;

    private Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success()
        => new(true, null);

    public static Result Failure(string error)
        => new(false, error);

    public static implicit operator Result<bool>(Result result)
        => result.IsSuccess
            ? Result<bool>.Success(true)
            : Result<bool>.Failure(result.Error!);
}
