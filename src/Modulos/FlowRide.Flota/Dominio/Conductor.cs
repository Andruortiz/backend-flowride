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

public sealed class Conductor : Entidad
{
    private readonly List<Turno> _turnos = [];

    private Conductor(Guid id, Guid usuarioId, DocumentoVigencia licencia)
        : base(id)
    {
        UsuarioId = usuarioId;
        Licencia = licencia;
        Activo = true;
        Disponible = true;
    }

    public Guid UsuarioId { get; }

    public DocumentoVigencia Licencia { get; private set; }

    public Guid? VehiculoId { get; private set; }

    public bool Activo { get; private set; }

    public bool Disponible { get; private set; }

    public string? MotivoNoDisponible { get; private set; }

    public IReadOnlyList<Turno> Turnos => _turnos;

    public static Conductor Crear(Usuario usuario, DocumentoVigencia licencia, Guid? id = null)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        ArgumentNullException.ThrowIfNull(licencia);

        if (usuario.Rol != RolUsuario.Conductor)
        {
            throw new DomainException("Solo un usuario con rol Conductor puede tener perfil de conductor.");
        }

        ValidarLicencia(licencia);

        return new Conductor(UuidHelper.GetOrGenerate(id), usuario.Id, licencia);
    }

    public void ActualizarLicencia(DocumentoVigencia licencia)
    {
        ArgumentNullException.ThrowIfNull(licencia);
        ValidarLicencia(licencia);

        Licencia = licencia;
    }

    public void AsignarVehiculo(Vehiculo vehiculo)
    {
        ArgumentNullException.ThrowIfNull(vehiculo);

        VehiculoId = vehiculo.Id;
    }

    public void QuitarVehiculo() => VehiculoId = null;

    public void Inactivar() => Activo = false;

    public void Activar() => Activo = true;

    public void MarcarNoDisponible(string motivo)
    {
        if (TextHelper.IsEmptyWithTrim(motivo))
        {
            throw new DomainException("Indica el motivo por el que el conductor no está disponible.");
        }

        Disponible = false;
        MotivoNoDisponible = motivo.Trim();
    }

    public void MarcarDisponible()
    {
        Disponible = true;
        MotivoNoDisponible = null;
    }

    public void DefinirTurnos(IEnumerable<Turno> turnos)
    {
        ArgumentNullException.ThrowIfNull(turnos);

        var nuevos = turnos.ToList();

        for (var i = 0; i < nuevos.Count; i++)
        {
            for (var j = i + 1; j < nuevos.Count; j++)
            {
                if (nuevos[i].SeSolapaCon(nuevos[j]))
                {
                    throw new DomainException("Los turnos del conductor no pueden solaparse.");
                }
            }
        }

        _turnos.Clear();
        _turnos.AddRange(nuevos);
    }

    public EvaluacionAsignabilidad EvaluarAsignabilidad(Vehiculo? vehiculo, DateOnly fecha, TimeOnly hora)
    {
        var motivos = new List<MotivoNoAsignable>();

        if (!Activo)
        {
            motivos.Add(MotivoNoAsignable.ConductorInactivo);
        }

        if (!Disponible)
        {
            motivos.Add(MotivoNoAsignable.ConductorNoDisponible);
        }

        if (!Licencia.EstaVigenteEn(fecha))
        {
            motivos.Add(MotivoNoAsignable.LicenciaNoVigente);
        }

        if (vehiculo is null)
        {
            motivos.Add(MotivoNoAsignable.SinVehiculo);
        }
        else if (!vehiculo.Habilitado)
        {
            motivos.Add(MotivoNoAsignable.VehiculoNoHabilitado);
        }
        else if (vehiculo.DocumentosNoVigentesEn(fecha).Count > 0)
        {
            motivos.Add(MotivoNoAsignable.DocumentosVehiculoNoVigentes);
        }

        if (!_turnos.Exists(turno => turno.Cubre(fecha, hora)))
        {
            motivos.Add(MotivoNoAsignable.FueraDeTurno);
        }

        return new EvaluacionAsignabilidad(motivos);
    }

    private static void ValidarLicencia(DocumentoVigencia licencia)
    {
        if (!licencia.Tipo.EsDeConductor())
        {
            throw new DomainException("El documento indicado no es una licencia de conducción.");
        }
    }
}
