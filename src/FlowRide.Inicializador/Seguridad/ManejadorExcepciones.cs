using FlowRide.Nucleo.Excepciones;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FlowRide.Inicializador.Seguridad;

/// <summary>Traduce las excepciones del sistema a ProblemDetails. Un error inesperado no filtra su mensaje.</summary>
internal sealed partial class ManejadorExcepciones(IProblemDetailsService problemas, ILogger<ManejadorExcepciones> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (estado, titulo, detalle) = exception switch
        {
            DomainException e => (StatusCodes.Status422UnprocessableEntity, "Regla de negocio", e.Message),
            NotFoundException e => (StatusCodes.Status404NotFound, "No encontrado", e.Message),
            ConflictException e => (StatusCodes.Status409Conflict, "Conflicto", e.Message),
            ForbiddenException e => (StatusCodes.Status403Forbidden, "Prohibido", e.Message),
            _ => (StatusCodes.Status500InternalServerError, "Error interno", "Ocurrió un error inesperado."),
        };

        if (estado == StatusCodes.Status500InternalServerError)
        {
            ErrorInesperado(logger, exception);
        }

        httpContext.Response.StatusCode = estado;

        return await problemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = estado, Title = titulo, Detail = detalle },
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Error no controlado")]
    private static partial void ErrorInesperado(ILogger logger, Exception ex);
}
