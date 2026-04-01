namespace AuthBackend.Domain.Exceptions;

public class CredencialesInvalidasException : Exception
{
    public CredencialesInvalidasException(string message) : base(message) { }
}
