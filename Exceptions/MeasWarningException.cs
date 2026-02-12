namespace TemperatureCharacteristics.Exceptions
{
    //*******************************************************
    //警告レベルの例外を表すクラス
    //致命的ではないが処理をスキップする必要がある場合に使用
    //*******************************************************
    public class MeasWarningException : Exception
    {
        public MeasWarningException(string message) : base(message) { }

        public MeasWarningException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}