using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Identidad.Dominio;
using FlowRide.Plataforma.Dominio;

namespace FlowRide.Flota.Dominio;

public static class TipoDocumentoExtensions
{
    public static bool EsDeConductor(this TipoDocumento tipo) =>
        tipo == TipoDocumento.LicenciaConduccion;

    public static bool EsDeVehiculo(this TipoDocumento tipo) =>
        tipo is TipoDocumento.Soat
            or TipoDocumento.RevisionTecnicoMecanica
            or TipoDocumento.TarjetaOperacion;
}
