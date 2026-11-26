namespace ConsultasPlan.Services;

public enum ServiceStatus
{
    Success,
    NotFound,
    Conflict,
    Invalid
}

public sealed record ServiceResult<T>(
    ServiceStatus Status,
    T? Data = default,
    string? Message = null)
{
    public static ServiceResult<T> Ok(T data) => new(ServiceStatus.Success, data);
    public static ServiceResult<T> NotFound(string message) => new(ServiceStatus.NotFound, default, message);
    public static ServiceResult<T> Conflict(string message) => new(ServiceStatus.Conflict, default, message);
    public static ServiceResult<T> Invalid(string message) => new(ServiceStatus.Invalid, default, message);
}
