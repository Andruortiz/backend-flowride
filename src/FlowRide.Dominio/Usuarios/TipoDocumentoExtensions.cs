namespace FlowRide.Dominio.Usuarios;

public static class TipoDocumentoExtensions
{
    public static bool EsDeConductor(this TipoDocumento tipo) =>
        tipo == TipoDocumento.LicenciaConduccion;

    public static bool EsDeVehiculo(this TipoDocumento tipo) =>
        tipo is TipoDocumento.Soat
            or TipoDocumento.RevisionTecnicoMecanica
            or TipoDocumento.TarjetaOperacion;
}
