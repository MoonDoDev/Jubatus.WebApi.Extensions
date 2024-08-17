using Destructurama.Attributed;

namespace Jubatus.WebApi.Extensions.Settings;

public interface IMongoSettings
{
    string? Host { get; init; }

    int Port { get; init; }

    string? ServiceName { get; init; }

    string? UserName { get; init; }

    [NotLogged]
    string? UserPass { get; init; }

    [NotLogged]
    string ConnectionString { get; }
}