namespace FlowRide.Nucleo.Excepciones;

public abstract class NegocioException : Exception
{
    protected NegocioException(string clave, Exception? interna = null, params (string Nombre, object? Valor)[] variables)
        : base(clave, interna)
    {
        Clave = clave;
        Variables = new MensajeClave(clave, variables).Variables;
        Detalles = [];
    }

    protected NegocioException(IReadOnlyList<MensajeClave> detalles)
        : base(detalles[0].Clave)
    {
        Clave = detalles[0].Clave;
        Variables = detalles[0].Variables;
        Detalles = detalles;
    }

    public string Clave { get; }

    public IReadOnlyDictionary<string, string> Variables { get; }

    public IReadOnlyList<MensajeClave> Detalles { get; }
}
