namespace Jubatus.WebApi.Extensions.Exceptions;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

/// <summary>
/// Manejador global de excepciones, el cual implementa la Interface "IExceptionHandler" 
/// y se adiciona a la Colección de Servicios de la WebApi que implemente este paquete. 
/// </summary>
/// <param name="logger">Instancia del ILogger "injectada" con DI.</param>
internal sealed class GeneralExceptionHandler(
    ILogger<GeneralExceptionHandler> logger ): IExceptionHandler
{
    private readonly ILogger<GeneralExceptionHandler> _logger = logger;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="exception"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken )
    {
        FastLogger.LogError( _logger, exception );

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error"
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync( problemDetails, cancellationToken ).ConfigureAwait( false );
        return true;
    }
}