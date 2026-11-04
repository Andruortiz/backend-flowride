using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Identidad.Dominio;
using FlowRide.Flota.Dominio;
using FlowRide.Plataforma.Dominio;

namespace FlowRide.Viajes.Dominio;

public sealed class Calificacion : Entidad
{
    public const int PuntajeMinimo = 1;
    public const int PuntajeMaximo = 5;
    public const int LongitudMaximaComentario = 500;

    private Calificacion(Guid id, Guid viajeId, Guid pasajeroId, Guid conductorId, int puntaje, string? comentario, DateTimeOffset creadaEn)
        : base(id)
    {
        ViajeId = viajeId;
        PasajeroId = pasajeroId;
        ConductorId = conductorId;
        Puntaje = puntaje;
        Comentario = comentario;
        CreadaEn = creadaEn;
    }

    public Guid ViajeId { get; }

    public Guid PasajeroId { get; }

    public Guid ConductorId { get; }

    public int Puntaje { get; }

    public string? Comentario { get; }

    public DateTimeOffset CreadaEn { get; }

    public static Calificacion Crear(Viaje viaje, Guid pasajeroId, int puntaje, string? comentario, DateTimeOffset ahora, Guid? id = null)
    {
        ArgumentNullException.ThrowIfNull(viaje);

        if (viaje.Estado != EstadoViaje.Finalizado)
        {
            throw new DomainException("error.solo_se_califica_un_viaje_finalizado");
        }

        if (viaje.PasajeroId != pasajeroId)
        {
            throw new DomainException("error.solo_el_pasajero_del_viaje_puede_calificarlo");
        }

        if (puntaje is < PuntajeMinimo or > PuntajeMaximo)
        {
            throw new DomainException("error.el_puntaje_debe_estar_entre_y", ("puntajeMinimo", PuntajeMinimo), ("puntajeMaximo", PuntajeMaximo));
        }

        var texto = TextHelper.IsEmptyWithTrim(comentario) ? null : comentario.Trim();

        if (texto is { Length: > LongitudMaximaComentario })
        {
            throw new DomainException("error.el_comentario_no_puede_superar_caracteres", ("longitudMaximaComentario", LongitudMaximaComentario));
        }

        return new Calificacion(UuidHelper.GetOrGenerate(id), viaje.Id, pasajeroId, viaje.ConductorId, puntaje, texto, ahora);
    }
}
