namespace FlowRide.Nucleo.Excepciones;

public class ForbiddenException : NegocioException
{
    public ForbiddenException(string clave, params (string Nombre, object? Valor)[] variables) : base(clave, null, variables)
    {
    }

    public ForbiddenException(string clave, Exception interna) : base(clave, interna)
    {
    }
}
