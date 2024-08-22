namespace Jubatus.WebApi.Extensions.Settings;
using Destructurama.Attributed;

/// <summary>
/// 
/// </summary>
public interface IMongoDbSettings
{
    string? Host { get; set; }

    int Port { get; set; }

    string? UserName { get; set; }

    [NotLogged]
    string? UserPass { get; set; }

    string? ServiceName { get; set; }

    string? CollectionName { get; set; }

    [NotLogged]
    string ConnectionString { get; }
}