namespace SchoolAdmission.Domain.Exceptions;

/// <summary>
/// Thrown when an operation would violate a business rule of an entity.
/// <see cref="Code"/> is a machine-readable error code the UI can map to a friendly message.
/// </summary>
public class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
