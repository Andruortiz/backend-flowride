namespace FlowRide.Borde;

public sealed class OpcionesBorde
{
    public const string Seccion = "Borde";

    public string AppSecret { get; set; } = "";

    public string VerifyToken { get; set; } = "";

    public List<OperadorBorde> Operadores { get; set; } = [];
}

public sealed class OperadorBorde
{
    public string PhoneNumberId { get; set; } = "";

    public string Destino { get; set; } = "";
}
