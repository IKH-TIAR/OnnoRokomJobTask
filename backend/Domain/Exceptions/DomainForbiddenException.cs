namespace SchoolAdmission.Domain.Exceptions;

/// <summary>The caller is not allowed to act on this entity (e.g. another student's hold). Maps to HTTP 403.</summary>
public class DomainForbiddenException(string code, string message) : DomainException(code, message);
