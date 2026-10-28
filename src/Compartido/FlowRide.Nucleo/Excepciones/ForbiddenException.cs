namespace FlowRide.Nucleo.Excepciones;

/// <summary>El actor autenticado no puede ejecutar la acción.</summary>
public class ForbiddenException : Exception
{
    public ForbiddenException()
    {
    }

    public ForbiddenException(string message) : base(message)
    {
    }

    public ForbiddenException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
