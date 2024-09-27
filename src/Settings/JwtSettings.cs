namespace Jubatus.WebApi.Extensions.Settings;

/// <summary>
/// 
/// </summary>
public sealed class JwtSettings: IJwtSettings
{
    /// <summary>
    /// 
    /// </summary>
    public string? JwtKey { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string? JwtIssuer { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string? JwtAudience { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string? ValidateIssuer { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string? ValidateAudience { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string? ValidateLifetime { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string? ValidateIssuerSigningKey { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string? AuthUser { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string? AuthPass { get; set; }
}
