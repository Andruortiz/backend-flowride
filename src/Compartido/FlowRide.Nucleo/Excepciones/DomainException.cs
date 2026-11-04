namespace FlowRide.Nucleo.Excepciones;

public class DomainException : NegocioException
{
    public DomainException(string clave, params (string Nombre, object? Valor)[] variables) : base(clave, null, variables)
    {
    }

    public DomainException(string clave, Exception interna) : base(clave, interna)
    {
    }

    public DomainException(IReadOnlyList<MensajeClave> detalles) : base(detalles)
    {
    }
}
