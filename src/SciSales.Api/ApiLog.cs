namespace SciSales.Api;

internal static partial class ApiLog
{
    [LoggerMessage(
        EventId = 5000,
        Level = LogLevel.Error,
        Message = "Unhandled exception on {Method} {Path}.")]
    public static partial void UnhandledException(ILogger logger, Exception exception, string method, string path);
}
