using System.Text;
using FlowRide.Clientes.Aplicacion;

namespace FlowRide.Pruebas.Dominio.Itinerarios;

public class LectorItinerarioCsvTests
{
    private const string Cabecera = "codigo_servicio,fecha,hora_salida,hora_llegada,destino";

    private static string Archivo(params string[] filas) => string.Join("\n", [Cabecera, .. filas]);

    [Fact]
    public void LeeLasFilasValidas()
    {
        var resultado = LectorItinerarioCsv.Leer(Archivo(
            "AV123,2026-11-03,04:30,05:30,Aeropuerto El Dorado",
            "AV456,2026-11-05,5:00,6:15,Aeropuerto El Dorado"));

        Assert.Empty(resultado.Errores);
        Assert.Equal(2, resultado.Filas.Count);
        Assert.Equal("AV123", resultado.Filas[0].CodigoServicio);
        Assert.Equal(new DateOnly(2026, 11, 3), resultado.Filas[0].Fecha);
        Assert.Equal(new TimeOnly(4, 30), resultado.Filas[0].HoraSalida);
        Assert.Equal(new TimeOnly(6, 15), resultado.Filas[1].HoraLlegada);
        Assert.Equal(3, resultado.Filas[1].Linea);
    }

    [Fact]
    public void AceptaPuntoYComaComillasBomYLineasEnBlanco()
    {
        var contenido = "﻿destino;codigo_servicio;fecha;hora_salida;hora_llegada\r\n\r\n\"Aeropuerto, Terminal 1\";AV9;2026-11-03;04:30;05:30\r\n";

        var resultado = LectorItinerarioCsv.Leer(contenido);

        Assert.Empty(resultado.Errores);
        Assert.Equal("Aeropuerto, Terminal 1", Assert.Single(resultado.Filas).Destino);
        Assert.Equal(3, resultado.Filas[0].Linea);
    }

    [Fact]
    public void UnArchivoVacioSeRechaza()
    {
        var resultado = LectorItinerarioCsv.Leer("  \n \n");

        Assert.Empty(resultado.Filas);
        Assert.Single(resultado.Errores);
    }

    [Fact]
    public void FaltandoUnaColumnaSeRechazaTodoElArchivo()
    {
        var resultado = LectorItinerarioCsv.Leer("codigo_servicio,fecha,hora_salida,destino\nAV1,2026-11-03,04:30,X");

        Assert.Empty(resultado.Filas);
        Assert.Contains("hora_llegada", Assert.Single(resultado.Errores).Mensaje, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(",2026-11-03,04:30,05:30,X", "código")]
    [InlineData("AV1,03/11/2026,04:30,05:30,X", "fecha")]
    [InlineData("AV1,2026-11-03,4h30,05:30,X", "salida")]
    [InlineData("AV1,2026-11-03,04:30,25:00,X", "llegada")]
    [InlineData("AV1,2026-11-03,04:30,05:30,", "destino")]
    [InlineData("AV1,2026-11-03,04:30,04:30,X", "iguales")]
    public void CadaErrorDeUnaFilaSeReportaConSuLinea(string fila, string pista)
    {
        var resultado = LectorItinerarioCsv.Leer(Archivo(fila));

        Assert.Empty(resultado.Filas);
        var error = Assert.Single(resultado.Errores);
        Assert.Equal(2, error.Linea);
        Assert.Contains(pista, error.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnaFilaConErrorNoImpideLeerLasDemas()
    {
        var resultado = LectorItinerarioCsv.Leer(Archivo(
            "AV1,2026-11-03,04:30,05:30,X",
            "AV2,fecha-mala,04:30,05:30,X",
            "AV3,2026-11-04,04:30,05:30,X"));

        Assert.Equal(2, resultado.Filas.Count);
        Assert.Equal(3, Assert.Single(resultado.Errores).Linea);
    }

    [Fact]
    public void UnViajeRepetidoEnElArchivoSeReportaUnaSolaVez()
    {
        var resultado = LectorItinerarioCsv.Leer(Archivo(
            "AV1,2026-11-03,04:30,05:30,X",
            "AV1,2026-11-03,06:00,07:00,X"));

        Assert.Single(resultado.Filas);
        Assert.Equal(3, Assert.Single(resultado.Errores).Linea);
    }

    [Fact]
    public void UnArchivoConDemasiadosViajesSeCorta()
    {
        var filas = new StringBuilder();

        for (var i = 0; i < LectorItinerarioCsv.MaximoFilas + 5; i++)
        {
            filas.Append(System.Globalization.CultureInfo.InvariantCulture, $"AV{i},2026-11-03,04:30,05:30,X\n");
        }

        var resultado = LectorItinerarioCsv.Leer(Cabecera + "\n" + filas);

        Assert.Equal(LectorItinerarioCsv.MaximoFilas, resultado.Filas.Count);
        Assert.Contains("máximo", Assert.Single(resultado.Errores).Mensaje, StringComparison.Ordinal);
    }
}
