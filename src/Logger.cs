using Destructurama;
using Serilog;

namespace Jubatus.WebApi.Extensions;

public static class Logger
{
    /// <summary>
    /// Creamos una instancia del Logger de Serilog (MinimumLevel -> Debug)
    /// NOTA: Se recomienda liberar los recursos de esta instancia, haciendo uso de la palabra reservada "using", ejemplo:
    /// -> using Serilog.Core.Logger log = GetLogger();
    /// </summary>
    /// <returns></returns>
    public static global::Serilog.Core.Logger GetLogger(LoggerMinLevel level) => level switch
    {
        LoggerMinLevel.Debug => new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().Destructure.UsingAttributes().CreateLogger(),
        LoggerMinLevel.Information => new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console().Destructure.UsingAttributes().CreateLogger(),
        LoggerMinLevel.Warning => new LoggerConfiguration().MinimumLevel.Warning().WriteTo.Console().Destructure.UsingAttributes().CreateLogger(),
        LoggerMinLevel.Error => new LoggerConfiguration().MinimumLevel.Error().WriteTo.Console().Destructure.UsingAttributes().CreateLogger(),
        LoggerMinLevel.Fatal => new LoggerConfiguration().MinimumLevel.Fatal().WriteTo.Console().Destructure.UsingAttributes().CreateLogger(),
        _ => new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Console().Destructure.UsingAttributes().CreateLogger(),
    };
}