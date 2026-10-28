using FlowRide.Clientes.Dominio;
using static FlowRide.Pruebas.Dominio.Escenarios;

using FlowRide.Comunicacion.Dominio;
using FlowRide.Contratos;
using FlowRide.Flota.Dominio;
using FlowRide.Identidad.Dominio;
using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Plataforma.Dominio;


namespace FlowRide.Pruebas.Dominio.Soporte;

public class SoporteTests
{
    [Theory]
    [InlineData(RolUsuario.Pasajero)]
    [InlineData(RolUsuario.Conductor)]
    public void PasajeroYConductorSoloPorWhatsApp(RolUsuario rol)
    {
        Assert.Equal([CanalNotificacion.WhatsApp], PoliticaCanal.Resolver(rol, critica: true));
    }

    [Fact]
    public void ElPanelEsParaAdministracionYLoCriticoAgregaCorreo()
    {
        Assert.Equal([CanalNotificacion.Panel], PoliticaCanal.Resolver(RolUsuario.OperadorLogistico));
        Assert.Equal(
            [CanalNotificacion.Panel, CanalNotificacion.Correo],
            PoliticaCanal.Resolver(RolUsuario.AdministradorOperador, critica: true));
    }

    [Fact]
    public void ElCatalogoRenderizaVariablesYRechazaClavesDesconocidas()
    {
        var texto = CatalogoMensajes.Renderizar(
            "asignacion.pasajero",
            new Dictionary<string, string> { ["fecha"] = "15 oct 08:30", ["conductor"] = "Carlos", ["placa"] = "ABC123" });

        Assert.Equal("Tu servicio del 15 oct 08:30 tiene conductor: Carlos, vehículo ABC123.", texto);
        Assert.Throws<KeyNotFoundException>(() => CatalogoMensajes.Renderizar("no.existe"));
    }

    [Fact]
    public void ElParametroValidaSuRangoYVersionaCadaCambio()
    {
        var parametro = ParametroOperador.Crear(CatalogoParametros.UmbralEtaMinutos, 5, Ahora);

        parametro.Actualizar(8, Id(1), Ahora.AddHours(1));

        Assert.Equal(8, parametro.Valor);
        Assert.Equal(2, parametro.Version);
        Assert.Equal(Id(1), parametro.ModificadoPor);
        Assert.Throws<DomainException>(() => parametro.Actualizar(0, null, Ahora));
        Assert.Throws<DomainException>(() => parametro.Actualizar(31, null, Ahora));
        Assert.Throws<DomainException>(() => ParametroOperador.Crear("no.existe", 1, Ahora));
    }

    [Fact]
    public void ElCatalogoTieneDefectosDentroDeSuRango()
    {
        Assert.All(CatalogoParametros.Definiciones, d => Assert.InRange(d.Defecto, d.Minimo, d.Maximo));
    }

    [Fact]
    public void LaAuditoriaExigeAccionYRecursoYAsumeSistemaSinActor()
    {
        var registro = RegistroAuditoria.Registrar(null, " ", "Reasignación", "V-1", ResultadoAuditoria.Aceptada, Ahora, "Motivo");

        Assert.Equal("Sistema", registro.ActorNombre);
        Assert.Throws<DomainException>(() => RegistroAuditoria.Registrar(null, "x", " ", "V-1", ResultadoAuditoria.Aceptada, Ahora));
    }

    [Fact]
    public void LaAlertaExigeClaveYSeMarcaLeidaUnaVez()
    {
        var alerta = AlertaOperativa.Crear("eta", SeveridadAlerta.Atencion, "ETA", "+7 min", "V-1", "eta:V-1", Ahora);

        alerta.MarcarLeida(Ahora.AddMinutes(1));
        alerta.MarcarLeida(Ahora.AddMinutes(9));

        Assert.Equal(Ahora.AddMinutes(1), alerta.LeidaEn);
        Assert.Throws<DomainException>(() => AlertaOperativa.Crear("eta", SeveridadAlerta.Atencion, "ETA", "", "", " ", Ahora));
    }

    [Fact]
    public void ElClienteEmpresarialExigeNombre()
    {
        var cliente = ClienteEmpresarial.Crear(" AeroNorte ", " ");

        Assert.Equal("AeroNorte", cliente.Nombre);
        Assert.Null(cliente.Nit);
        Assert.Throws<DomainException>(() => ClienteEmpresarial.Crear(" ", null));
    }
}
