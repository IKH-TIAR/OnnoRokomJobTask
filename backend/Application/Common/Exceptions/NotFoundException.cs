namespace SchoolAdmission.Application.Common.Exceptions;

public class NotFoundException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
