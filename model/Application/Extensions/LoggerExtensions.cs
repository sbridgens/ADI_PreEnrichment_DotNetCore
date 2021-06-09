using System;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;

namespace Application.Extensions
{
    public static class LoggerExtensions
    {
        public static Result LogErrorResult<T>(this ILogger<T> logger, Exception ex, string message)
        {
            logger.LogError(ex, message);
            return Result.Failure(message);
        }

        public static Result LogErrorWithInnerException<T>(this ILogger<T> logger, Exception exception, 
            string functionName, string message)
        {
            logger.LogError($"[{functionName}] {message} : {exception.Message}");
            if (exception.InnerException != null)
                logger.LogError($"Inner Exception: {exception.InnerException.Message}");
            return Result.Failure(message);
        }
    }
}