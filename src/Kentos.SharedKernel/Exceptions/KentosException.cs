namespace Kentos.SharedKernel.Exceptions;

/// <summary>
/// Base for all known (expected) application errors. The global exception handler
/// converts them to ProblemDetails via <see cref="StatusCode"/> and <see cref="ErrorCode"/>.
/// </summary>
public abstract class KentosException : Exception
{
    protected KentosException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }

    /// <summary>Machine-readable error code, e.g. "not_found".</summary>
    public abstract string ErrorCode { get; }

    /// <summary>HTTP status code.</summary>
    public abstract int StatusCode { get; }
}

/// <summary>Requested resource was not found (404).</summary>
public sealed class NotFoundException : KentosException
{
    public NotFoundException(string message) : base(message)
    {
    }

    public override string ErrorCode => "not_found";
    public override int StatusCode => 404;

    public static NotFoundException For(string entity, object key) =>
        new($"{entity} bulunamadı: {key}");
}

/// <summary>Conflict / concurrency / uniqueness violation (409).</summary>
public sealed class ConflictException : KentosException
{
    public ConflictException(string message) : base(message)
    {
    }

    public override string ErrorCode => "conflict";
    public override int StatusCode => 409;
}

/// <summary>Business rule violation (422).</summary>
public sealed class BusinessRuleException : KentosException
{
    public BusinessRuleException(string message) : base(message)
    {
    }

    public override string ErrorCode => "business_rule";
    public override int StatusCode => 422;
}

/// <summary>Authorization denied (403).</summary>
public sealed class ForbiddenException : KentosException
{
    public ForbiddenException(string message = "Bu işlem için yetkiniz yok.") : base(message)
    {
    }

    public override string ErrorCode => "forbidden";
    public override int StatusCode => 403;
}
