namespace FlowRide.Persistencia;

public static class Paginacion
{
    public const int TamanoMaximo = 200;

    public static int Tomar(int tamano) => Math.Clamp(tamano, 1, TamanoMaximo);

    public static int Saltar(int pagina, int tamano) => (Math.Max(pagina, 1) - 1) * Tomar(tamano);
}
