using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Persistencia;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FlowRide.Persistencia;

internal sealed class UnitOfWork(FlowRideDbContext db) : IUnitOfWork
{
    public async Task GuardarCambiosAsync(CancellationToken ct = default)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflictException("error.otro_usuario_modifico_el_mismo_registro_vuelve", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("error.la_operacion_choca_con_un_registro_existente", ex);
        }
    }
}
