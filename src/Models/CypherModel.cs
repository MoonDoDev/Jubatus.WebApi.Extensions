namespace Jubatus.WebApi.Extensions.Models;

/// <summary>
/// 
/// </summary>
public record CypherModel: ICypherModel
{
    /// <summary>
    /// 
    /// </summary>
    public string AliasName { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    public string UserPass { get; set; } = string.Empty;
}