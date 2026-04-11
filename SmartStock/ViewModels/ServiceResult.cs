namespace SmartStock.ViewModels
{
    public class ServiceResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<string> Errors { get; set; } = new();

        public static ServiceResult Ok(string? message = null) =>
            new() { Success = true, Message = message };

        public static ServiceResult Fail(string error) =>
            new() { Success = false, Errors = [error] };

        public static ServiceResult Fail(IEnumerable<string> errors) =>
            new() { Success = false, Errors = errors.ToList() };
    }

    public class ServiceResult<T> : ServiceResult
    {
        public T? Data { get; set; }

        public static ServiceResult<T> Ok(T data, string? message = null) =>
            new() { Success = true, Data = data, Message = message };

        public new static ServiceResult<T> Fail(string error) =>
            new() { Success = false, Errors = [error] };
    }
}
