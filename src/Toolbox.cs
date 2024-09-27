namespace Jubatus.WebApi.Extensions;

using Jubatus.WebApi.Extensions.Models;
using Jubatus.WebApi.Extensions.Settings;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

/// <summary>
/// 
/// </summary>
public static class Toolbox
{
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
                    new Claim( ClaimTypes.Name, userData.AliasName ?? "" )
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