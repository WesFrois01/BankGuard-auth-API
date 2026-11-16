namespace BankGuard_Auth_API.Common;

public class ServiceResult<T>
{
    public bool IsSuccess { get; private set; }
    public int StatusCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public T? Data { get; private set; }

    public static ServiceResult<T> Ok(T data, int statusCode = 200) => new()
    {
        IsSuccess = true,
        StatusCode = statusCode,
        Data = data
    };

    public static ServiceResult<T> Fail(string errorMessage, int statusCode = 400) => new()
    {
        IsSuccess = false,
        StatusCode = statusCode,
        ErrorMessage = errorMessage
    };
}
