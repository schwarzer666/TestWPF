namespace TemperatureCharacteristics.Services.Results
{
    public class Result<T> : Result
    {
        public T Data { get; }

        public Result(bool success, string message, T data)
            : base(success, message)
        {
            Data = data;
        }

        public static Result<T> Ok(T data, string message = "")
            => new Result<T>(true, message, data);

        public static Result<T> Fail(string message)
            => new Result<T>(false, message, default!);
    }
}
