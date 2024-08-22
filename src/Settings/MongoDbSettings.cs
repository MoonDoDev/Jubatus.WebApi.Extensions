namespace Jubatus.WebApi.Extensions.Settings;
using Destructurama.Attributed;

/// <summary>
/// 
/// </summary>
public sealed class MongoDbSettings: IMongoDbSettings
{
    public string? Host { get; set; }

    public int Port { get; set; }

    public string? UserName { get; set; }

    [NotLogged]
    public string? UserPass { get; set; }

    public string? ServiceName { get; set; }

    public string? CollectionName { get; set; }

    [NotLogged]
    public string ConnectionString => $"mongodb://{UserName}:{UserPass}@{Host}:{Port}";
}
