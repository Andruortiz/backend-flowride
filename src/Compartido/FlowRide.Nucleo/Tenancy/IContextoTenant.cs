namespace FlowRide.Nucleo.Tenancy;

/// <summary>Operador (tenant) al que sirve este despliegue. El aislamiento se decide al desplegar (Silo).</summary>
public interface IContextoTenant
{
    Guid TenantId { get; }
}
