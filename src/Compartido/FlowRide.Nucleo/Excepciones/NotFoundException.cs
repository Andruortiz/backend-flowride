namespace FlowRide.Nucleo.Excepciones;

public class NotFoundException : NegocioException
{
    public NotFoundException(string clave, params (string Nombre, object? Valor)[] variables) : base(clave, null, variables)
    {
    }

    public NotFoundException(string clave, Exception interna) : base(clave, interna)
    {
    }
}
