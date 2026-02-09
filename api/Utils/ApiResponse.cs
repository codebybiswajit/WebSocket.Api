using MongoDB.Driver.Linq;
using System.Text.Json.Serialization;

using System;
using Newtonsoft.Json;
namespace api.Utils
{
    public class ApiResponse<T>
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        private readonly DateTime? time;
        
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public double ExecutionTime => time.HasValue ? DateTime.Now.Subtract(time.Value).TotalSeconds : 0.0;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Success { get; set; }
        
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Message { get; set; }
       
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public T? Result { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int? StatusCode { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public ErrorMessage? Errors { get; set; }

        public ApiResponse() { }

        // Success factory
        public  ApiResponse<T> AddResult(T data, string? message = null, int? statusCode = 200 )
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
                }
            };
    }

    public class ErrorMessage
    {
        public string? Message { get; set; }

    }
}
