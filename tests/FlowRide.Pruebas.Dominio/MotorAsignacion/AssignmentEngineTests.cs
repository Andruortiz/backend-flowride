using static FlowRide.Pruebas.Dominio.Escenarios;

using FlowRide.Asignaciones.Dominio;
using FlowRide.Contratos;
using FlowRide.Flota.Dominio;
using FlowRide.Identidad.Dominio;
using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Seguridad;


namespace FlowRide.Pruebas.Dominio.MotorAsignacion;

public class AssignmentEngineTests
{
    private readonly AssignmentEngine _motor = new();
    private readonly ConfiguracionAsignacion _config = ConfiguracionAsignacion.PorDefecto(Ahora);

    private static ContextoAsignacion Contexto(bool urgente = false) => new(Solicitud(urgente), Fecha, Hora);

    [Fact]
    public void EligeAlConductorConMenosCarga()
    {
        var candidatos = new[] { Candidato(1, carga: 4), Candidato(2, carga: 1), Candidato(3, carga: 2) };

        var resultado = _motor.Asignar(Contexto(), candidatos, _config);

        Assert.True(resultado.Exitoso);
        Assert.Equal(Id(2), resultado.Ganador!.Conductor.Id);
        Assert.Equal(Id(2), resultado.Decision.ConductorElegido);
        Assert.Equal(3, resultado.Decision.Ranking.Count);
    }

    [Fact]
    public void MismaEntradaMismoResultado()
    {
        var candidatos = new[] { Candidato(5, 1, 1), Candidato(3, 1, 1), Candidato(4, 0, 2) };

        var a = _motor.Asignar(Contexto(), candidatos, _config);
        var b = _motor.Asignar(Contexto(), [.. candidatos.Reverse()], _config);

        Assert.Equal(a.Decision.ConductorElegido, b.Decision.ConductorElegido);
        Assert.Equal(a.Decision.Ranking.Select(r => (r.ConductorId, r.Total)), b.Decision.Ranking.Select(r => (r.ConductorId, r.Total)));
    }

    [Fact]
    public void ElEmpateSeResuelveConElIdentificadorEstable()
    {
        var candidatos = new[] { Candidato(7), Candidato(2), Candidato(9) };

        var resultado = _motor.Asignar(Contexto(), candidatos, _config);

        Assert.Equal(Id(2), resultado.Decision.ConductorElegido);
        Assert.Contains("identificador estable", resultado.Decision.CriterioDesempate);
    }

    [Fact]
    public void LasReglasEliminatoriasDescartanConMotivo()
    {
        var inactivo = Candidato(1);
        inactivo.Conductor.Inactivar();
        var noDisponible = Candidato(2);
        noDisponible.Conductor.MarcarNoDisponible("Descanso");
        var licenciaVencida = new CandidatoAsignacion(ConductorListo(3, venceLicencia: Fecha.AddDays(-1)), VehiculoAlDia("CCC333"), 0, 0);
        var sinVehiculo = new CandidatoAsignacion(ConductorListo(4), null, 0, 0);
        var docsVencidos = new CandidatoAsignacion(ConductorListo(5), VehiculoAlDia("DDD444", vence: Fecha.AddDays(-2)), 0, 0);
        var valido = Candidato(6);

        var resultado = _motor.Asignar(Contexto(), [inactivo, noDisponible, licenciaVencida, sinVehiculo, docsVencidos, valido], _config);

        Assert.Equal(Id(6), resultado.Ganador!.Conductor.Id);
        Assert.Equal(5, resultado.Decision.Descartados.Count);
        Assert.Contains(resultado.Decision.Descartados, d => d.ConductorId == Id(1) && d.Motivo.Contains("inactivo"));
        Assert.Contains(resultado.Decision.Descartados, d => d.ConductorId == Id(3) && d.Motivo.Contains("Licencia"));
        Assert.Contains(resultado.Decision.Descartados, d => d.ConductorId == Id(4) && d.Motivo.Contains("vehículo"));
        Assert.Contains(resultado.Decision.Descartados, d => d.ConductorId == Id(5) && d.Motivo.Contains("Documentos"));
    }

    [Fact]
    public void FueraDeTurnoQuedaDescartado()
    {
        var contexto = new ContextoAsignacion(Solicitud(), Fecha, new TimeOnly(15, 0));

        var resultado = _motor.Asignar(contexto, [Candidato(1)], _config);

        Assert.False(resultado.Exitoso);
        Assert.Contains(resultado.Decision.Descartados, d => d.Regla == "Compatibilidad de turno");
    }

    [Fact]
    public void SinCandidatosValidosNoHayGanador()
    {
        var resultado = _motor.Asignar(Contexto(), [], _config);

        Assert.False(resultado.Exitoso);
        Assert.Null(resultado.Decision.ConductorElegido);
    }

    [Fact]
    public void ElServicioUrgentePrefiereAQuienEstaLibre()
    {
        var libre = Candidato(8, carga: 0, recientes: 3);
        var ocupado = Candidato(1, carga: 1, recientes: 0);

        var normal = _motor.Asignar(Contexto(urgente: false), [libre, ocupado], _config);
        var urgente = _motor.Asignar(Contexto(urgente: true), [libre, ocupado], _config);

        Assert.Equal(Id(1), normal.Ganador!.Conductor.Id);
        Assert.Equal(Id(8), urgente.Ganador!.Conductor.Id);
    }

    [Fact]
    public void LaCapacidadDescartaVehiculosPequenos()
    {
        var contexto = new ContextoAsignacion(Solicitud(), Fecha, Hora, PasajerosRequeridos: 3);

        var resultado = _motor.Asignar(contexto, [Candidato(1), Candidato(2, vehiculo: VehiculoAlDia("BBB222", capacidad: 4))], _config);

        Assert.Equal(Id(2), resultado.Ganador!.Conductor.Id);
    }

    [Fact]
    public void ElOrdenYLosPesosCambianElResultado()
    {
        var pocaCarga = Candidato(1, carga: 0, recientes: 4);
        var pocasRecientes = Candidato(2, carga: 4, recientes: 0);
        var soloCarga = ConfiguracionAsignacion.Crear(2, ConfigConPesos(carga: 1, recientes: 0), null, Ahora);
        var soloRecientes = ConfiguracionAsignacion.Crear(3, ConfigConPesos(carga: 0, recientes: 1), null, Ahora);

        Assert.Equal(Id(1), _motor.Asignar(Contexto(), [pocaCarga, pocasRecientes], soloCarga).Ganador!.Conductor.Id);
        Assert.Equal(Id(2), _motor.Asignar(Contexto(), [pocaCarga, pocasRecientes], soloRecientes).Ganador!.Conductor.Id);
        Assert.Equal(3, _motor.Asignar(Contexto(), [pocaCarga], soloRecientes).Decision.VersionConfiguracion);
    }

    [Fact]
    public void LaDecisionExplicaLasReglasAplicadasYElDesglose()
    {
        var resultado = _motor.Asignar(Contexto(), [Candidato(1, carga: 1)], _config);

        Assert.Contains("Disponibilidad", resultado.Decision.ReglasAplicadas);
        var puntaje = Assert.Single(resultado.Decision.Ranking);
        Assert.Contains(puntaje.Desglose, d => d.Regla == "Balance de carga" && d.Puntos == 51);
        Assert.Equal(1, resultado.Decision.VersionConfiguracion);
    }

    [Fact]
    public void NoSePuedeDesactivarUnaReglaObligatoria()
    {
        var reglas = _config.Reglas.Select(r => r.ReglaId == "regulatoria" ? r with { Activa = false } : r);

        var error = Assert.Throws<DomainException>(() => ConfiguracionAsignacion.Crear(2, reglas, null, Ahora));

        Assert.Contains("obligatoria", error.Message);
    }

    [Fact]
    public void ElValidadorRechazaDuplicadosPesosFueraDeRangoYSinPuntuacion()
    {
        var duplicada = _config.Reglas.Append(new ConfiguracionRegla("balance_carga", true, 99, 0.5));
        var pesoInvalido = _config.Reglas.Select(r => r.ReglaId == "balance_carga" ? r with { Peso = 1.5 } : r);
        var sinPuntuacion = _config.Reglas.Select(r => r.ReglaId is "balance_carga" or "recientes" ? r with { Activa = false } : r);
        var desconocida = _config.Reglas.Append(new ConfiguracionRegla("magia", true, 50, 1));
        var ordenRepetido = _config.Reglas.Select(r => r.ReglaId == "recientes" ? r with { Orden = 1 } : r);

        Assert.Throws<DomainException>(() => ConfiguracionAsignacion.Crear(2, duplicada, null, Ahora));
        Assert.Throws<DomainException>(() => ConfiguracionAsignacion.Crear(2, pesoInvalido, null, Ahora));
        Assert.Throws<DomainException>(() => ConfiguracionAsignacion.Crear(2, sinPuntuacion, null, Ahora));
        Assert.Throws<DomainException>(() => ConfiguracionAsignacion.Crear(2, desconocida, null, Ahora));
        Assert.Throws<DomainException>(() => ConfiguracionAsignacion.Crear(2, ordenRepetido, null, Ahora));
    }

    private IEnumerable<ConfiguracionRegla> ConfigConPesos(double carga, double recientes) =>
        _config.Reglas.Select(r => r.ReglaId switch
        {
            "balance_carga" => r with { Peso = carga },
            "recientes" => r with { Peso = recientes },
            _ => r,
        });
}

public class AsignacionTests
{
    private static Asignacion Nueva() => Asignacion.Crear(Id(1), Id(2), Id(3), 1, null, Ahora);

    [Fact]
    public void CrearQuedaPendienteDeAceptacionYEmiteElEvento()
    {
        var asignacion = Nueva();

        Assert.Equal(EstadoAsignacion.PendienteAceptacion, asignacion.Estado);
        Assert.True(asignacion.EsActiva);
        Assert.IsType<AsignacionCreada>(Assert.Single(asignacion.EventosDominio));
    }

    [Fact]
    public void AceptarYLuegoIniciarYFinalizar()
    {
        var asignacion = Nueva();

        asignacion.Aceptar(Ahora);
        asignacion.Iniciar();
        asignacion.Finalizar();

        Assert.Equal(EstadoAsignacion.Finalizada, asignacion.Estado);
        Assert.False(asignacion.EsActiva);
        Assert.Contains(asignacion.EventosDominio, e => e is AsignacionAceptada);
    }

    [Fact]
    public void NoSeAceptaDosVecesNiSeRechazaLoAceptado()
    {
        var asignacion = Nueva();
        asignacion.Aceptar(Ahora);

        Assert.Throws<DomainException>(() => asignacion.Aceptar(Ahora));
        Assert.Throws<DomainException>(() => asignacion.Rechazar(Ahora));
    }

    [Fact]
    public void RechazarEmiteElEventoYLaDejaInactiva()
    {
        var asignacion = Nueva();

        asignacion.Rechazar(Ahora);

        Assert.Equal(EstadoAsignacion.Rechazada, asignacion.Estado);
        Assert.False(asignacion.EsActiva);
        Assert.Contains(asignacion.EventosDominio, e => e is AsignacionRechazada);
    }

    [Fact]
    public void ReasignarDejaLaAnteriorEnElHistoricoYEmiteElEvento()
    {
        var anterior = Nueva();
        var nueva = Asignacion.CrearManual(Id(2), Id(8), Id(9), 1, "Conductor enfermo", Ahora);

        anterior.Reasignar(nueva, "Conductor enfermo", Ahora);

        Assert.Equal(EstadoAsignacion.Reasignada, anterior.Estado);
        Assert.True(nueva.EsManual);
        var evento = anterior.EventosDominio.OfType<AsignacionReasignada>().Single();
        Assert.Equal(Id(8), evento.ConductorNuevoId);
    }

    [Fact]
    public void LaReasignacionManualExigeMotivoYUnaAsignacionEnCursoNoSeReasigna()
    {
        Assert.Throws<DomainException>(() => Asignacion.CrearManual(Id(2), Id(8), Id(9), 1, " ", Ahora));

        var enCurso = Nueva();
        enCurso.Aceptar(Ahora);
        enCurso.Iniciar();

        Assert.Throws<DomainException>(() => enCurso.Reasignar(Nueva(), "Tarde", Ahora));
    }
}
