namespace AuthBackend.Domain.Exceptions;

public class TokenInvalidoException : Exception
{
    public TokenInvalidoException(string message) : base(message) { }
}
