namespace AuthBackend.Domain.Exceptions;

public class UsuarioYaExisteException : Exception
{
    public UsuarioYaExisteException(string message) : base(message) { }
}
