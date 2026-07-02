namespace MultiVendorAPI.Common
{
    public class ServiceResponse<T>
    {
        public bool Success { get; set; }

        public int StatusCode { get; set; }

        public string Message { get; set; } = string.Empty;

        public T? Data { get; set; }

        public static ServiceResponse<T> SuccessResponse(
            T data,
            string message = "Success",
            int statusCode = 200)
        {
            return new ServiceResponse<T>
            {
                Success = true,
                StatusCode = statusCode,
                Message = message,
                Data = data
            };
        }

        public static ServiceResponse<T> FailureResponse(
            string message,
            int statusCode = 400)
        {
            return new ServiceResponse<T>
            {
                Success = false,
                StatusCode = statusCode,
                Message = message,
                Data = default
            };
        }
    }
}