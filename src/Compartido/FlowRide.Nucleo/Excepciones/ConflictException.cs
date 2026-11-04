namespace FlowRide.Nucleo.Excepciones;

public class ConflictException : NegocioException
{
    public ConflictException(string clave, params (string Nombre, object? Valor)[] variables) : base(clave, null, variables)
    {
    }

    public ConflictException(string clave, Exception interna) : base(clave, interna)
    {
    }
}
