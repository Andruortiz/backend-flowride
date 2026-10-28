using System.Reflection;
using FlowRide.Nucleo.Dominio;
using Microsoft.AspNetCore.Mvc;
using NetArchTest.Rules;

namespace FlowRide.Pruebas.Arquitectura;

/// <summary>
/// Monolito modular (ADR-0001): cada módulo es un proyecto con Dominio, Aplicacion e Infraestructura.
/// Los módulos solo dependen de los que tienen debajo y se hablan por puertos de consulta, servicios o eventos.
/// </summary>
public class ReglasDeDependencia
{
    // Quién puede depender de quién (de abajo hacia arriba). Cualquier otra referencia entre módulos es un error.
    private static readonly Dictionary<string, string[]> Grafo = new()
    {
        ["Plataforma"] = [],
        ["Clientes"] = ["Plataforma"],
        ["Identidad"] = ["Clientes", "Plataforma"],
        ["Flota"] = ["Identidad", "Plataforma"],
        ["Solicitudes"] = ["Identidad", "Plataforma"],
        ["Viajes"] = ["Identidad", "Flota", "Plataforma"],
        ["Asignaciones"] = ["Clientes", "Flota", "Solicitudes", "Viajes", "Identidad", "Plataforma"],
        ["Comunicacion"] = ["Identidad", "Flota", "Solicitudes", "Asignaciones", "Viajes", "Plataforma"],
        ["Operacion"] = ["Plataforma", "Clientes", "Identidad", "Flota", "Solicitudes", "Viajes", "Asignaciones", "Comunicacion"],
    };

    private static readonly string[] Frameworks = ["Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Npgsql"];

    public static IEnumerable<object[]> Modulos() => Grafo.Keys.Select(m => new object[] { m });

    private static Assembly Ensamblado(string modulo) => Assembly.Load($"FlowRide.{modulo}");

    private static readonly Assembly Nucleo = typeof(Entidad).Assembly;
    private static readonly Assembly Contratos = typeof(FlowRide.Contratos.UsuarioDesactivado).Assembly;
    private static readonly Assembly Persistencia = typeof(FlowRide.Persistencia.FlowRideDbContext).Assembly;

    private static void SinDependencias(Assembly ensamblado, string descripcion, params string[] prohibidos)
    {
        var resultado = Types.InAssembly(ensamblado).ShouldNot().HaveDependencyOnAny(prohibidos).GetResult();

        Assert.True(resultado.IsSuccessful, $"{descripcion}: {string.Join(", ", resultado.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void NucleoNoDependeDeNingunModuloNiDePersistencia() =>
        SinDependencias(Nucleo, "Nucleo", ["FlowRide.Contratos", "FlowRide.Persistencia", "FlowRide.Inicializador", .. Grafo.Keys.Select(m => $"FlowRide.{m}")]);

    [Fact]
    public void ContratosSoloDependenDelNucleo() =>
        SinDependencias(Contratos, "Contratos", ["FlowRide.Persistencia", "FlowRide.Inicializador", .. Grafo.Keys.Select(m => $"FlowRide.{m}")]);

    [Fact]
    public void PersistenciaNoConoceANingunModulo() =>
        SinDependencias(Persistencia, "Persistencia", ["FlowRide.Inicializador", .. Grafo.Keys.Select(m => $"FlowRide.{m}")]);

    [Theory]
    [MemberData(nameof(Modulos))]
    public void ElDominioDeUnModuloNoUsaSuAplicacionNiSuInfraestructuraNiFrameworks(string modulo)
    {
        var resultado = Types.InAssembly(Ensamblado(modulo))
            .That().ResideInNamespace($"FlowRide.{modulo}.Dominio")
            .ShouldNot().HaveDependencyOnAny([$"FlowRide.{modulo}.Aplicacion", $"FlowRide.{modulo}.Infraestructura", "FlowRide.Persistencia", .. Frameworks])
            .GetResult();

        Assert.True(resultado.IsSuccessful, string.Join(", ", resultado.FailingTypeNames ?? []));
    }

    [Theory]
    [MemberData(nameof(Modulos))]
    public void LaAplicacionDeUnModuloNoUsaSuInfraestructuraNiFrameworks(string modulo)
    {
        var resultado = Types.InAssembly(Ensamblado(modulo))
            .That().ResideInNamespace($"FlowRide.{modulo}.Aplicacion")
            .ShouldNot().HaveDependencyOnAny([$"FlowRide.{modulo}.Infraestructura", "FlowRide.Persistencia", .. Frameworks])
            .GetResult();

        Assert.True(resultado.IsSuccessful, string.Join(", ", resultado.FailingTypeNames ?? []));
    }

    [Theory]
    [MemberData(nameof(Modulos))]
    public void UnModuloSoloReferenciaALosModulosPermitidos(string modulo)
    {
        var referenciados = Ensamblado(modulo).GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(n => n.StartsWith("FlowRide.", StringComparison.Ordinal))
            .Select(n => n["FlowRide.".Length..])
            .Where(Grafo.ContainsKey)
            .ToList();

        var sobran = referenciados.Except(Grafo[modulo]).ToList();

        Assert.True(sobran.Count == 0, $"{modulo} referencia módulos no permitidos: {string.Join(", ", sobran)}");
    }

    [Theory]
    [MemberData(nameof(Modulos))]
    public void UnModuloNoUsaLaInfraestructuraDeOtroModulo(string modulo)
    {
        var otros = Grafo.Keys.Where(m => m != modulo).Select(m => $"FlowRide.{m}.Infraestructura").ToArray();
        var resultado = Types.InAssembly(Ensamblado(modulo)).ShouldNot().HaveDependencyOnAny(otros).GetResult();

        Assert.True(resultado.IsSuccessful, string.Join(", ", resultado.FailingTypeNames ?? []));
    }

    [Fact]
    public void LosControladoresVivenSoloEnInfraestructuraControladores()
    {
        var infractores = Grafo.Keys
            .SelectMany(m => Ensamblado(m).GetTypes().Where(t => typeof(ControllerBase).IsAssignableFrom(t) && t.Namespace != $"FlowRide.{m}.Infraestructura.Controladores"))
            .Select(t => t.FullName)
            .ToList();

        Assert.True(infractores.Count == 0, string.Join(", ", infractores));
    }

    [Fact]
    public void LosRepositoriosSonInternosYVivenEnInfraestructuraRepositorios()
    {
        var infractores = Grafo.Keys
            .SelectMany(m => Ensamblado(m).GetTypes()
                .Where(t => t.Name.EndsWith("Repository", StringComparison.Ordinal) && !t.IsInterface)
                .Where(t => t.IsPublic || t.Namespace != $"FlowRide.{m}.Infraestructura.Repositorios"))
            .Select(t => t.FullName)
            .ToList();

        Assert.True(infractores.Count == 0, string.Join(", ", infractores));
    }

    [Fact]
    public void LosPuertosDeSalidaNoSonPublicos()
    {
        var infractores = Grafo.Keys
            .SelectMany(m => Ensamblado(m).GetTypes()
                .Where(t => t.IsInterface && t.IsPublic && t.Namespace == $"FlowRide.{m}.Aplicacion.Puertos")
                .Where(t => t.Name.EndsWith("Repository", StringComparison.Ordinal)))
            .Select(t => t.FullName)
            .ToList();

        Assert.True(infractores.Count == 0, $"Repositorios públicos (otro módulo podría usarlos): {string.Join(", ", infractores)}");
    }

    [Fact]
    public void LasEntidadesDelDominioNoExponenSettersPublicos()
    {
        var infractores = Grafo.Keys
            .SelectMany(m => Ensamblado(m).GetTypes().Where(t => typeof(Entidad).IsAssignableFrom(t) && !t.IsAbstract))
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(p => p.SetMethod is { IsPublic: true } && !IsInit(p))
                .Select(p => $"{t.Name}.{p.Name}"))
            .ToList();

        Assert.True(infractores.Count == 0, $"Setters públicos en el dominio: {string.Join(", ", infractores)}");
    }

    private static bool IsInit(PropertyInfo propiedad) =>
        propiedad.SetMethod!.ReturnParameter.GetRequiredCustomModifiers()
            .Any(m => m.FullName == "System.Runtime.CompilerServices.IsExternalInit");

    [Fact]
    public void LosEventosDeDominioSonSelladosYVivenEnContratos()
    {
        var resultado = Types.InAssemblies([Nucleo, Contratos, .. Grafo.Keys.Select(Ensamblado)])
            .That().ImplementInterface(typeof(IEventoDominio)).And().AreNotAbstract()
            .Should().BeSealed().And().ResideInNamespace("FlowRide.Contratos")
            .GetResult();

        Assert.True(resultado.IsSuccessful, string.Join(", ", resultado.FailingTypeNames ?? []));
    }
}
