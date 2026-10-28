using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Persistencia;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FlowRide.Persistencia;

/// <summary>Confirma agregados y outbox en una transacción y traduce los choques de concurrencia a <see cref="ConflictException"/>.</summary>
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
            throw new ConflictException("Otro usuario modificó el mismo registro; vuelve a consultarlo e inténtalo de nuevo.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("La operación choca con un registro existente o con otra operación simultánea.", ex);
        }
    }
}
