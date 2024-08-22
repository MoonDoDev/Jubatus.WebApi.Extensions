namespace Jubatus.WebApi.Extensions;
using Destructurama;
using Jubatus.WebApi.Extensions.Models;
using Jubatus.WebApi.Extensions.Settings;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using Serilog;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

/// <summary>
/// 
/// </summary>
public enum LoggerMinLevel
{
    Verbose = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Fatal = 5
}

/// <summary>
/// 
/// </summary>
public static class Toolbox
{
    /// <summary>
    /// Creamos una instancia del Logger de Serilog (MinimumLevel -> Debug)
    /// NOTA: Se recomienda liberar los recursos de esta instancia, haciendo uso de la palabra reservada "using", ejemplo:
    /// -> using Serilog.Core.Logger log = GetLogger();
    /// </summary>
    /// <returns></returns>
    public static Serilog.Core.Logger GetLogger( LoggerMinLevel level ) => level switch
    {
        LoggerMinLevel.Verbose => new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Console().Destructure.UsingAttributes().CreateLogger(),
        LoggerMinLevel.Debug => new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().Destructure.UsingAttributes().CreateLogger(),
        LoggerMinLevel.Information => new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console().Destructure.UsingAttributes().CreateLogger(),
        LoggerMinLevel.Warning => new LoggerConfiguration().MinimumLevel.Warning().WriteTo.Console().Destructure.UsingAttributes().CreateLogger(),
        LoggerMinLevel.Error => new LoggerConfiguration().MinimumLevel.Error().WriteTo.Console().Destructure.UsingAttributes().CreateLogger(),
        _ => new LoggerConfiguration().MinimumLevel.Fatal().WriteTo.Console().Destructure.UsingAttributes().CreateLogger(),
    };

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cypherData"></param>
    /// <param name="configuration"></param>
    /// <param name="configSectionName"></param>
    /// <returns></returns>
    public static string EncryptUserPassword(
        this ICypherModel cypherData,
        IConfiguration configuration,
        string configSectionName = "JwtSettings" )
    {
        ArgumentNullException.ThrowIfNull( cypherData );
        ArgumentNullException.ThrowIfNull( configuration );

        var jwtOptions = new JwtSettings();
        configuration.GetSection( configSectionName ).Bind( jwtOptions );

        var iv = new byte[16];
        byte[] array;

        using( var aes = Aes.Create() )
        {
            aes.Key = Encoding.UTF8.GetBytes( jwtOptions.JwtKey! );
            aes.IV = iv;

#pragma warning disable CA5401 // Do not use CreateEncryptor with non-default IV
#pragma warning disable S3329 // Cipher Block Chaining IVs should be unpredictable

            var encryptor = aes.CreateEncryptor( aes.Key, aes.IV );

#pragma warning restore S3329 // Cipher Block Chaining IVs should be unpredictable
#pragma warning restore CA5401 // Do not use CreateEncryptor with non-default IV

            using MemoryStream memoryStream = new();
            using CryptoStream cryptoStream = new( ( Stream ) memoryStream, encryptor, CryptoStreamMode.Write );
            using( StreamWriter streamWriter = new( ( Stream ) cryptoStream ) )
            {
                streamWriter.WriteAsync( cypherData.UserPass );
            }

            array = memoryStream.ToArray();
        }

        return Convert.ToBase64String( array );
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userData"></param>
    /// <param name="configuration"></param>
    /// <param name="configSectionName"></param>
    /// <returns></returns>
    public static TokensModel? GenerateBearerToken(
        ICypherModel userData,
        IConfiguration configuration,
        string configSectionName = "JwtSettings" )
    {
        ArgumentNullException.ThrowIfNull( userData );
        ArgumentNullException.ThrowIfNull( configuration );

        var jwtOptions = new JwtSettings();
        configuration.GetSection( configSectionName ).Bind( jwtOptions );

        var authPassword = userData.EncryptUserPassword( configuration, configSectionName );

        if( jwtOptions.AuthUser == userData.AliasName && jwtOptions.AuthPass == authPassword )
        {
            var tokenHandle = new JwtSecurityTokenHandler();
            var tokenKey = Encoding.UTF8.GetBytes( jwtOptions.JwtKey! );

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, userData.AliasName??"")
                ] ),
                Expires = DateTime.UtcNow.AddMinutes( 10 ),
                SigningCredentials = new SigningCredentials( new SymmetricSecurityKey( tokenKey ), SecurityAlgorithms.HmacSha256Signature )
            };

            var token = tokenHandle.CreateToken( tokenDescriptor );
            return new TokensModel { Token = tokenHandle.WriteToken( token ) };
        }

        return default;
    }
}