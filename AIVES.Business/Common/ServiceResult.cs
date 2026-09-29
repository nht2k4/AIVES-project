namespace AIVES.Business.Common;

public enum ServiceErrorType
{
    None,
    Validation,
    NotFound,
    Forbidden
}

// Kết quả trả về từ tầng Business: Controller chỉ cần đọc, không cần biết luật nghiệp vụ bên trong
public class ServiceResult
{
    public ServiceErrorType ErrorType { get; protected init; }
    public string? Error { get; protected init; }
    public bool Succeeded => ErrorType == ServiceErrorType.None;

    public static ServiceResult Ok() => new();
    public static ServiceResult Fail(string error) => new() { ErrorType = ServiceErrorType.Validation, Error = error };
    public static ServiceResult NotFound(string error) => new() { ErrorType = ServiceErrorType.NotFound, Error = error };
    public static ServiceResult Forbidden(string error) => new() { ErrorType = ServiceErrorType.Forbidden, Error = error };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; private init; }

    public static ServiceResult<T> Ok(T data) => new() { Data = data };
    public new static ServiceResult<T> Fail(string error) => new() { ErrorType = ServiceErrorType.Validation, Error = error };
    public new static ServiceResult<T> NotFound(string error) => new() { ErrorType = ServiceErrorType.NotFound, Error = error };
    public new static ServiceResult<T> Forbidden(string error) => new() { ErrorType = ServiceErrorType.Forbidden, Error = error };
}
