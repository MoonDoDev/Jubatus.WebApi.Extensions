namespace Jubatus.WebApi.Extensions.Settings;

/// <summary>
/// 
/// </summary>
public interface IMongoDbSettings
{
    string? Host { get; set; }

    int Port { get; set; }

    string? UserName { get; set; }

    string? UserPass { get; set; }

    string? ServiceName { get; set; }

    string? CollectionName { get; set; }

    string ConnectionString { get; }
}