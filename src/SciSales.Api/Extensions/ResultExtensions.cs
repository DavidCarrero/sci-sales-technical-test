using SciSales.Domain.Common;

namespace SciSales.Api.Extensions;

/// <summary>
/// Turns a domain failure into an HTTP answer, in one place. Endpoints never
/// decide status codes on their own, so the API cannot drift into returning 400
/// for one kind of not-found and 404 for another.
/// </summary>
internal static class ResultExtensions
{
    public static IResult ToProblem(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            throw new InvalidOperationException("A successful result is not a problem.");
        }

        var error = result.Error;

        var (statusCode, title) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Invalid request"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Resource not found"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            ErrorType.Unavailable => (StatusCodes.Status503ServiceUnavailable, "Upstream service unavailable"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error"),
        };

        // The message is written for a human and carries no table names, paths or
        // connection details; the stable "code" is what a client should branch on.
        return Results.Problem(
            title: title,
            detail: error.Message,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
