namespace TemperatureCharacteristics.Exceptions
{
    //*******************************************************
    //致命的レベルの例外を表すクラス
    //処理を中断すべき場合に使用
    //*******************************************************
    public class MeasFatalException : Exception
    {
        public MeasFatalException(string message) : base(message) { }

        public MeasFatalException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}


