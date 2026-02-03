using MongoDB.Driver.Linq;

namespace api.Utils
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Result { get; set; }
        public int? StatusCode { get; set; }
        public ErrorMessage? Errors { get; set; }

        public ApiResponse() { }

        // Success factory
        public ApiResponse<T> AddResult(T data, string? message = null, int? statusCode = 200)
            => new ApiResponse<T>
            {
                Success = true,
                Result = data,
                Message = message,
                StatusCode = statusCode,
                Errors = null
            };

        public  ApiResponse<T> AddError(string? message = null, int? statusCode = 400, string? errorMessage = null)
            => new ApiResponse<T>
            {
                Success = false,
                Result = default,
                Message = message ?? "Request failed",
                StatusCode = statusCode,
                Errors = new ErrorMessage
                {
                    Message = errorMessage ?? message,
                    Errors = null
                }
            };

        public ApiResponse<T> AddError(string? message, int statusCode, ErrorMessage? errors)
            => new ApiResponse<T>
            {
                Success = false,
                Result = default,
                Message = message ?? "Request failed",
                StatusCode = statusCode,
                Errors = new ErrorMessage
                {
                    Message = message,
                    Errors = new ErrorMessage
                    {
                        Message = errors?.Message,
                        Errors = errors?.Errors,
                    },
                }
            };
    }

    public class ErrorMessage
    {
        public string? Message { get; set; }
        public ErrorMessage? Errors { get; set; } = new();
    }
}
