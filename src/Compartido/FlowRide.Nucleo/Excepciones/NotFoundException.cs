namespace FlowRide.Nucleo.Excepciones;

/// <summary>El recurso pedido no existe en este operador.</summary>
public class NotFoundException : Exception
{
    public NotFoundException()
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
