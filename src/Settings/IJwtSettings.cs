namespace Jubatus.WebApi.Extensions.Settings;
using Destructurama.Attributed;

/// <summary>
/// 
/// </summary>
public interface IJwtSettings
{
    /// <summary>
    /// 
    /// </summary>
    [NotLogged]
    string? JwtKey { get; set; }

    /// <summary>
    /// 
    /// </summary>
    string? JwtIssuer { get; set; }

    /// <summary>
    /// 
    /// </summary>
    string? JwtAudience { get; set; }

    /// <summary>
    /// 
    /// </summary>
    string? ValidateIssuer { get; set; }

    /// <summary>
    /// 
    /// </summary>
    string? ValidateAudience { get; set; }

    /// <summary>
    /// 
    /// </summary>
    string? ValidateLifetime { get; set; }

    /// <summary>
    /// 
    /// </summary>
    string? ValidateIssuerSigningKey { get; set; }

    /// <summary>
    /// 
    /// </summary>
    string? AuthUser { get; set; }

    /// <summary>
    /// 
    /// </summary>
    string? AuthPass { get; set; }
}
