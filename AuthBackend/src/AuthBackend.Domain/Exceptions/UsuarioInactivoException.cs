namespace AuthBackend.Domain.Exceptions;

public class UsuarioInactivoException : Exception
{
    public UsuarioInactivoException(string message) : base(message) { }
}
