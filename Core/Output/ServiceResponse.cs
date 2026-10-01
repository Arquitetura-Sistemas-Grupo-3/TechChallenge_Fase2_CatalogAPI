namespace Core.Output
{
    public class ServiceResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }

        public static ServiceResponse Ok(string? message = null)
            => new ServiceResponse { Success = true, Message = message };

        public static ServiceResponse Fail(string message)
            => new ServiceResponse { Success = false, Message = message };
    }

    public class ServiceResponse<T> : ServiceResponse
    {
        public T? Data { get; set; }

        public static ServiceResponse<T> Ok(T data, string? message = null)
            => new ServiceResponse<T> { Success = true, Data = data, Message = message };

        public static new ServiceResponse<T> Fail(string message)
            => new ServiceResponse<T> { Success = false, Message = message };
    }
}
