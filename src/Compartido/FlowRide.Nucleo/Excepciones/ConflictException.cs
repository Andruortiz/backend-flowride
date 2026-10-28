namespace FlowRide.Nucleo.Excepciones;

/// <summary>Conflicto de estado o de concurrencia: otro proceso cambió el recurso o ya existe.</summary>
public class ConflictException : Exception
{
    public ConflictException()
    {
    }

    public ConflictException(string message) : base(message)
    {
    }

    public ConflictException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
