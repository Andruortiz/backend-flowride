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

public sealed record Turno
{
    private const int MinutosPorDia = 24 * 60;
    private const int MinutosPorSemana = 7 * MinutosPorDia;

    private static readonly int[] Desplazamientos = [-MinutosPorSemana, 0, MinutosPorSemana];

    private Turno(DayOfWeek dia, TimeOnly inicio, TimeOnly fin)
    {
        Dia = dia;
        Inicio = inicio;
        Fin = fin;
    }

    public DayOfWeek Dia { get; }

    public TimeOnly Inicio { get; }

    public TimeOnly Fin { get; }

    public bool CruzaMedianoche => Fin < Inicio;

    public static Turno Crear(DayOfWeek dia, TimeOnly inicio, TimeOnly fin)
    {
        if (!Enum.IsDefined(dia))
        {
            throw new DomainException("El día del turno no es válido.");
        }

        if (inicio == fin)
        {
            throw new DomainException("El inicio y el fin del turno no pueden ser iguales.");
        }

        return new Turno(dia, inicio, fin);
    }

    public bool Cubre(DateOnly fecha, TimeOnly hora)
    {
        if (!CruzaMedianoche)
        {
            return fecha.DayOfWeek == Dia && hora >= Inicio && hora < Fin;
        }

        var diaSiguiente = (DayOfWeek)(((int)Dia + 1) % 7);

        return (fecha.DayOfWeek == Dia && hora >= Inicio)
            || (fecha.DayOfWeek == diaSiguiente && hora < Fin);
    }

    public bool SeSolapaCon(Turno otro)
    {
        ArgumentNullException.ThrowIfNull(otro);

        var (inicioA, finA) = EnMinutosDeSemana();
        var (inicioB, finB) = otro.EnMinutosDeSemana();

        foreach (var desplazamiento in Desplazamientos)
        {
            if (inicioA < finB + desplazamiento && inicioB + desplazamiento < finA)
            {
                return true;
            }
        }

        return false;
    }

    private (int Inicio, int Fin) EnMinutosDeSemana()
    {
        var minutosInicio = (Inicio.Hour * 60) + Inicio.Minute;
        var minutosFin = (Fin.Hour * 60) + Fin.Minute;
        var duracion = CruzaMedianoche
            ? MinutosPorDia - minutosInicio + minutosFin
            : minutosFin - minutosInicio;
        var inicioSemana = ((int)Dia * MinutosPorDia) + minutosInicio;

        return (inicioSemana, inicioSemana + duracion);
    }
}
