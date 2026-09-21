namespace SciSales.Domain.Common;

/// <summary>
/// A business failure. <see cref="Code"/> is stable and safe to switch on;
/// <see cref="Message"/> is meant to be read by a human.
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error Validation(string code, string message) =>
        new(code, message, ErrorType.Validation);

    public static Error NotFound(string code, string message) =>
        new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict);

    public static Error Unavailable(string code, string message) =>
        new(code, message, ErrorType.Unavailable);
}

public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unavailable = 4,
}
