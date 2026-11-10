namespace SchoolAdmission.Domain.Exceptions;

/// <summary>
/// A business rule rejected the operation because of the current state of the system
/// (e.g. no seat left, student already applied). Maps to HTTP 409.
/// </summary>
public class DomainConflictException(string code, string message) : DomainException(code, message);
