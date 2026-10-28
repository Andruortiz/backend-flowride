using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion;
using FlowRide.Plataforma.Aplicacion.Dtos;

namespace FlowRide.Plataforma.Aplicacion.Puertos;

internal interface IParametroRepository
{
    Task<IReadOnlyList<ParametroOperador>> ListarAsync(CancellationToken ct = default);

    Task<ParametroOperador?> ObtenerAsync(string clave, CancellationToken ct = default);

    Task AgregarAsync(ParametroOperador parametro, CancellationToken ct = default);
}

internal interface IAuditoriaRepository
{
    Task AgregarAsync(RegistroAuditoria registro, CancellationToken ct = default);

    Task<IReadOnlyList<RegistroAuditoria>> ConsultarAsync(
        string? accion,
        string? recurso,
        DateTimeOffset? desde,
        DateTimeOffset? hasta,
        int pagina,
        int tamano,
        CancellationToken ct = default);
}

internal interface IOperadorRepository
{
    Task<Operador?> ObtenerAsync(CancellationToken ct = default);

    Task AgregarAsync(Operador operador, CancellationToken ct = default);
}
