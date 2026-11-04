using FlowRide.Nucleo.Persistencia;
using FlowRide.Plataforma.Aplicacion.Puertos;
using NSubstitute;

namespace FlowRide.Pruebas.Servicios;

internal sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => ahora;
}

internal sealed class DoblesComunes
{
    public static readonly DateTimeOffset Ahora = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    public IUnitOfWork Unidad { get; } = Substitute.For<IUnitOfWork>();

    public IServicioAuditoria Auditoria { get; } = Substitute.For<IServicioAuditoria>();

    public IServicioParametros Parametros { get; } = Substitute.For<IServicioParametros>();

    public IServicioOperador Operador { get; } = Substitute.For<IServicioOperador>();

    public TimeProvider Reloj { get; } = new RelojFijo(Ahora);

    public DoblesComunes()
    {
        Parametros.ValorAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(30);
        Operador.ZonaAsync(Arg.Any<CancellationToken>()).Returns(TimeZoneInfo.Utc);
    }

    public async Task VerificarGuardadoAsync(string accion)
    {
        await Auditoria.Received(1).RegistrarAsync(accion, Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await Unidad.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }
}
