namespace Common;

public readonly struct Result<T>
{
    private readonly bool _success;
    public readonly T? Value;
    public readonly ErrorModel? Error;

    private Result(T? v, ErrorModel? e, bool success)
    {
        Value = v;
        Error = e;
        _success = success;
    }

    public bool IsOk => _success;

    public static Result<T> Ok(T v)
    {
        return new(v, default, true);
    }

    public static Result<T> Err(ErrorModel e)
    {
        return new(default, e, false);
    }

    public static implicit operator Result<T>(T v) => new(v, default, true);
    public static implicit operator Result<T>(ErrorModel e) => new(default, e, false);

    public R Match<R>(
            Func<T, R> success,
            Func<ErrorModel, R> failure) =>
        _success ? success(Value!) : failure(Error!);
}
