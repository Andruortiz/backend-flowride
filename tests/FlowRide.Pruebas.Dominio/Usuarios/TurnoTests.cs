
using FlowRide.Contratos;
using FlowRide.Flota.Dominio;
using FlowRide.Identidad.Dominio;
using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Seguridad;

namespace FlowRide.Pruebas.Dominio.Usuarios;

public class TurnoTests
{
    private static readonly DateOnly Miercoles = new(2026, 10, 14);
    private static readonly DateOnly Jueves = new(2026, 10, 15);

    private static Turno Manana() =>
        Turno.Crear(DayOfWeek.Wednesday, new TimeOnly(5, 0), new TimeOnly(13, 0));

    private static Turno Noche() =>
        Turno.Crear(DayOfWeek.Wednesday, new TimeOnly(21, 0), new TimeOnly(5, 0));

    [Fact]
    public void CrearConInicioIgualAFinFalla()
    {
        Assert.Throws<DomainException>(() =>
            Turno.Crear(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(8, 0)));
    }

    [Fact]
    public void CrearConDiaInexistenteFalla()
    {
        Assert.Throws<DomainException>(() =>
            Turno.Crear((DayOfWeek)9, new TimeOnly(8, 0), new TimeOnly(9, 0)));
    }

    [Fact]
    public void ElTurnoDiurnoCubreDentroDelHorarioConInicioInclusivoYFinExclusivo()
    {
        var turno = Manana();

        Assert.True(turno.Cubre(Miercoles, new TimeOnly(5, 0)));
        Assert.True(turno.Cubre(Miercoles, new TimeOnly(12, 59)));
        Assert.False(turno.Cubre(Miercoles, new TimeOnly(13, 0)));
        Assert.False(turno.Cubre(Miercoles, new TimeOnly(4, 59)));
    }

    [Fact]
    public void ElTurnoNoCubreOtroDiaDeLaSemana()
    {
        Assert.False(Manana().Cubre(Jueves, new TimeOnly(8, 0)));
    }

    [Fact]
    public void ElTurnoNocturnoCruzaLaMedianocheYPerteneceAlDiaEnQueEmpieza()
    {
        var turno = Noche();

        Assert.True(turno.CruzaMedianoche);
        Assert.True(turno.Cubre(Miercoles, new TimeOnly(23, 0)));
        Assert.True(turno.Cubre(Jueves, new TimeOnly(2, 0)));
        Assert.False(turno.Cubre(Jueves, new TimeOnly(5, 0)));
        Assert.False(turno.Cubre(Miercoles, new TimeOnly(2, 0)));
    }

    [Fact]
    public void TurnosDeDistintoDiaNoSeSolapan()
    {
        var lunes = Turno.Crear(DayOfWeek.Monday, new TimeOnly(5, 0), new TimeOnly(13, 0));

        Assert.False(Manana().SeSolapaCon(lunes));
    }

    [Fact]
    public void TurnosContiguosNoSeSolapan()
    {
        var tarde = Turno.Crear(DayOfWeek.Wednesday, new TimeOnly(13, 0), new TimeOnly(21, 0));

        Assert.False(Manana().SeSolapaCon(tarde));
    }

    [Fact]
    public void TurnosQueComparten30MinutosSeSolapan()
    {
        var otro = Turno.Crear(DayOfWeek.Wednesday, new TimeOnly(12, 30), new TimeOnly(18, 0));

        Assert.True(Manana().SeSolapaCon(otro));
    }

    [Fact]
    public void UnTurnoNocturnoSeSolapaConElTurnoDiurnoDelDiaSiguiente()
    {
        var jueves = Turno.Crear(DayOfWeek.Thursday, new TimeOnly(4, 0), new TimeOnly(10, 0));

        Assert.True(Noche().SeSolapaCon(jueves));
    }

    [Fact]
    public void ElTurnoDelDomingoNocheSeSolapaConElDelLunesMadrugada()
    {
        var domingo = Turno.Crear(DayOfWeek.Sunday, new TimeOnly(22, 0), new TimeOnly(6, 0));
        var lunes = Turno.Crear(DayOfWeek.Monday, new TimeOnly(5, 0), new TimeOnly(9, 0));

        Assert.True(domingo.SeSolapaCon(lunes));
    }

    [Fact]
    public void DosTurnosConLosMismosDatosSonIguales()
    {
        Assert.Equal(Manana(), Manana());
    }
}
