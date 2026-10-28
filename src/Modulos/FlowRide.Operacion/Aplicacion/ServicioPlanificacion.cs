using System.Globalization;
using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Operacion.Aplicacion.Dtos;
using FlowRide.Operacion.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;
using FlowRide.Clientes.Dominio;
using FlowRide.Clientes.Aplicacion.Dtos;
using FlowRide.Clientes.Aplicacion.Puertos;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Flota.Dominio;
using FlowRide.Flota.Aplicacion.Dtos;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Solicitudes.Dominio;
using FlowRide.Solicitudes.Aplicacion.Dtos;
using FlowRide.Solicitudes.Aplicacion.Puertos;
using FlowRide.Viajes.Dominio;
using FlowRide.Viajes.Aplicacion.Dtos;
using FlowRide.Viajes.Aplicacion.Puertos;
using FlowRide.Asignaciones.Dominio;
using FlowRide.Asignaciones.Aplicacion.Dtos;
using FlowRide.Asignaciones.Aplicacion.Puertos;
using FlowRide.Comunicacion.Dominio;
using FlowRide.Comunicacion.Aplicacion.Dtos;
using FlowRide.Comunicacion.Aplicacion.Puertos;

using FlowRide.Plataforma.Aplicacion;

namespace FlowRide.Operacion.Aplicacion;

internal sealed class ServicioPlanificacion(
    IConsultaSolicitudes solicitudes,
    IConsultaFlota flota,
    IServicioParametros parametros,
    IServicioOperador operador,
    IServicioAlertas alertas,
    IUnitOfWork unidad,
    TimeProvider reloj) : IServicioPlanificacion
{
    public async Task<ResultadoPlanificacion> EjecutarAsync(CancellationToken ct = default)
    {
        var ahora = reloj.GetUtcNow();

        var recordatorios = await EmitirRecordatoriosAsync(ahora, ct);
        var sinConductor = await AlertarSinConductorAsync(ahora, ct);
        var documentos = await AlertarDocumentosAsync(ct);

        return new ResultadoPlanificacion(recordatorios, sinConductor, documentos);
    }

    private async Task<int> EmitirRecordatoriosAsync(DateTimeOffset ahora, CancellationToken ct)
    {
        var anticipacion = await parametros.ValorAsync(CatalogoParametros.AnticipacionRecordatorioMinutos, ct);
        var pendientes = await solicitudes.ListarParaRecordatorioAsync(ahora, ahora.AddMinutes(anticipacion), ct);

        foreach (var solicitud in pendientes)
        {
            solicitud.RegistrarRecordatorioDebido(ahora);
        }

        if (pendientes.Count > 0)
        {
            await unidad.GuardarCambiosAsync(ct);
        }

        return pendientes.Count;
    }

    private async Task<int> AlertarSinConductorAsync(DateTimeOffset ahora, CancellationToken ct)
    {
        var minutos = await parametros.ValorAsync(CatalogoParametros.AlertaSinConductorMinutos, ct);
        var abiertas = await solicitudes.ListarAbiertasAntesDeAsync(ahora.AddMinutes(-minutos), ct);

        foreach (var solicitud in abiertas)
        {
            await alertas.CrearAsync(
                "sin_conductor_espera",
                SeveridadAlerta.Critica,
                "Solicitud esperando conductor",
                $"Lleva más de {minutos} minutos sin conductor.",
                $"solicitud/{solicitud.Id}",
                $"espera:{solicitud.Id}",
                ct);
        }

        return abiertas.Count;
    }

    private async Task<int> AlertarDocumentosAsync(CancellationToken ct)
    {
        var hoy = Fechas.Hoy(reloj, await operador.ZonaAsync(ct));
        var aviso = await parametros.ValorAsync(CatalogoParametros.DiasAvisoDocumentos, ct);
        var cantidad = 0;

        foreach (var vehiculo in await flota.ListarVehiculosAsync(ct))
        {
            foreach (var documento in vehiculo.Documentos.Where(d => d.EstadoEn(hoy, aviso) != EstadoVigencia.Vigente))
            {
                await AlertarDocumentoAsync($"vehículo {vehiculo.Placa}", $"vehiculo/{vehiculo.Id}", documento, hoy, aviso, ct);
                cantidad++;
            }
        }

        foreach (var conductor in await flota.ListarConductoresAsync(true, ct))
        {
            if (conductor.Licencia.EstadoEn(hoy, aviso) != EstadoVigencia.Vigente)
            {
                await AlertarDocumentoAsync("licencia de conductor", $"conductor/{conductor.Id}", conductor.Licencia, hoy, aviso, ct);
                cantidad++;
            }
        }

        return cantidad;
    }

    private Task AlertarDocumentoAsync(string sujeto, string referencia, DocumentoVigencia documento, DateOnly hoy, int aviso, CancellationToken ct)
    {
        var estado = documento.EstadoEn(hoy, aviso);
        var severidad = estado == EstadoVigencia.Vencido ? SeveridadAlerta.Critica : SeveridadAlerta.Atencion;
        var detalle = estado == EstadoVigencia.Vencido
            ? $"El documento {documento.Tipo} de {sujeto} venció el {documento.VenceEl:yyyy-MM-dd}."
            : $"El documento {documento.Tipo} de {sujeto} vence en {documento.DiasParaVencer(hoy).ToString(CultureInfo.InvariantCulture)} días.";

        return alertas.CrearAsync(
            "documento", severidad, "Documento por renovar", detalle, referencia, $"doc:{referencia}:{documento.Tipo}:{documento.VenceEl:yyyyMMdd}:{estado}", ct);
    }
}
