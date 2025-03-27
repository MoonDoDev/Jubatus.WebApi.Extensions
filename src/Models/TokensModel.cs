namespace Jubatus.WebApi.Extensions.Models;

/// <summary>
/// 
/// </summary>
public record TokensModel
{
    /// <summary>
    /// 
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}
