using Destructurama.Attributed;

namespace Jubatus.WebApi.Extensions.Settings;

/// <summary>
/// 
/// </summary>
public interface IJwtSettings
{
    /// <summary>
    /// 
    /// </summary>
    [NotLogged]
    string? JwtKey { get; init; }

    /// <summary>
    /// 
    /// </summary>
    string? JwtIssuer { get; init; }

    /// <summary>
    /// 
    /// </summary>
    string? JwtAudience { get; init; }

    /// <summary>
    /// 
    /// </summary>
    string? ValidateIssuer { get; init; }

    /// <summary>
    /// 
    /// </summary>
    string? ValidateAudience { get; init; }

    /// <summary>
    /// 
    /// </summary>
    string? ValidateLifetime { get; init; }

    /// <summary>
    /// 
    /// </summary>
    string? ValidateIssuerSigningKey { get; init; }
}
