namespace TemperatureCharacteristics.Services.Results
{
    public class Result
    {
        public bool Success { get; }
        public string Message { get; }

        public Result(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public static Result Ok(string message = "")
            => new Result(true, message);

        public static Result Fail(string message)
            => new Result(false, message);
    }
}
