namespace FlowRide.Mensajes;

public interface ICatalogoMensajes
{
    string Texto(string clave, IReadOnlyDictionary<string, string>? variables = null);
}
