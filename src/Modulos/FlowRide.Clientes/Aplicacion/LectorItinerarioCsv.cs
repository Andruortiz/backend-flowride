using System.Globalization;
using System.Text;

namespace FlowRide.Clientes.Aplicacion;

public sealed record FilaItinerario(int Linea, string CodigoServicio, DateOnly Fecha, TimeOnly HoraSalida, TimeOnly HoraLlegada, string Destino);

public sealed record ErrorFila(int Linea, string Mensaje);

public sealed record ResultadoLectura(IReadOnlyList<FilaItinerario> Filas, IReadOnlyList<ErrorFila> Errores);

public static class LectorItinerarioCsv
{
    public const int MaximoFilas = 500;

    private static readonly string[] Columnas = ["codigo_servicio", "fecha", "hora_salida", "hora_llegada", "destino"];
    private static readonly string[] FormatosHora = ["H:mm", "HH:mm"];

    public static ResultadoLectura Leer(string contenido)
    {
        ArgumentNullException.ThrowIfNull(contenido);

        var lineas = contenido.TrimStart('﻿').Split('\n');
        var primera = Array.FindIndex(lineas, l => !string.IsNullOrWhiteSpace(l));

        if (primera < 0)
        {
            return Rechazo(1, "El archivo está vacío.");
        }

        var cabecera = lineas[primera].TrimEnd('\r');
        var separador = cabecera.Contains(';', StringComparison.Ordinal) && !cabecera.Contains(',', StringComparison.Ordinal) ? ';' : ',';
        var nombres = Dividir(cabecera, separador).Select(c => c.Trim().ToLowerInvariant()).ToList();
        var posiciones = Columnas.ToDictionary(c => c, nombres.IndexOf);
        var faltantes = Columnas.Where(c => posiciones[c] < 0).ToList();

        if (faltantes.Count > 0)
        {
            return Rechazo(primera + 1, $"Faltan las columnas: {string.Join(", ", faltantes)}.");
        }

        var filas = new List<FilaItinerario>();
        var errores = new List<ErrorFila>();
        var vistos = new HashSet<(string Codigo, DateOnly Fecha)>();
        var total = 0;

        for (var i = primera + 1; i < lineas.Length; i++)
        {
            var texto = lineas[i].TrimEnd('\r');

            if (string.IsNullOrWhiteSpace(texto))
            {
                continue;
            }

            if (++total > MaximoFilas)
            {
                errores.Add(new ErrorFila(i + 1, $"El archivo supera el máximo de {MaximoFilas} viajes."));

                break;
            }

            var error = LeerFila(i + 1, Dividir(texto, separador), posiciones, vistos, out var fila);

            if (error is null)
            {
                filas.Add(fila!);
            }
            else
            {
                errores.Add(new ErrorFila(i + 1, error));
            }
        }

        return new ResultadoLectura(filas, errores);
    }

    private static ResultadoLectura Rechazo(int linea, string mensaje) => new([], [new ErrorFila(linea, mensaje)]);

    private static string? LeerFila(
        int linea, List<string> celdas, Dictionary<string, int> posiciones, HashSet<(string Codigo, DateOnly Fecha)> vistos, out FilaItinerario? fila)
    {
        fila = null;

        var codigo = Celda(celdas, posiciones["codigo_servicio"]);
        var destino = Celda(celdas, posiciones["destino"]);

        if (codigo.Length == 0)
        {
            return "El código de servicio es obligatorio.";
        }

        if (codigo.Length > 50)
        {
            return "El código de servicio no puede superar 50 caracteres.";
        }

        if (!DateOnly.TryParseExact(Celda(celdas, posiciones["fecha"]), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
        {
            return "La fecha debe tener formato yyyy-MM-dd.";
        }

        if (!TimeOnly.TryParseExact(Celda(celdas, posiciones["hora_salida"]), FormatosHora, CultureInfo.InvariantCulture, DateTimeStyles.None, out var salida))
        {
            return "La hora de salida debe tener formato HH:mm.";
        }

        if (!TimeOnly.TryParseExact(Celda(celdas, posiciones["hora_llegada"]), FormatosHora, CultureInfo.InvariantCulture, DateTimeStyles.None, out var llegada))
        {
            return "La hora de llegada debe tener formato HH:mm.";
        }

        if (destino.Length == 0)
        {
            return "El destino es obligatorio.";
        }

        if (destino.Length > 300)
        {
            return "El destino no puede superar 300 caracteres.";
        }

        if (salida == llegada)
        {
            return "La hora de salida y la de llegada no pueden ser iguales.";
        }

        if (!vistos.Add((codigo, fecha)))
        {
            return "Viaje repetido en el archivo (mismo código y fecha).";
        }

        fila = new FilaItinerario(linea, codigo, fecha, salida, llegada, destino);

        return null;
    }

    private static string Celda(List<string> celdas, int posicion) => posicion < celdas.Count ? celdas[posicion].Trim() : string.Empty;

    private static List<string> Dividir(string linea, char separador)
    {
        var celdas = new List<string>();
        var actual = new StringBuilder();
        var entreComillas = false;

        for (var i = 0; i < linea.Length; i++)
        {
            var c = linea[i];

            if (c == '"' && entreComillas && i + 1 < linea.Length && linea[i + 1] == '"')
            {
                actual.Append('"');
                i++;
            }
            else if (c == '"')
            {
                entreComillas = !entreComillas;
            }
            else if (c == separador && !entreComillas)
            {
                celdas.Add(actual.ToString());
                actual.Clear();
            }
            else
            {
                actual.Append(c);
            }
        }

        celdas.Add(actual.ToString());

        return celdas;
    }
}
