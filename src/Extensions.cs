using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Jubatus.WebApi.Extensions.MongoDB;
using MongoDB.Driver;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Jubatus.WebApi.Extensions.Settings;
using Microsoft.OpenApi.Models;

namespace Jubatus.WebApi.Extensions;

/// <summary>
/// En esta clase estática estaremos definiendo las extensiones requeridas en el Sistema y aplicables a Json Web Token
/// </summary>
public static class ExtensionsJwt
{
    private const string SECURITY_DEFINITION_NAME = "Bearer";

    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="jwtSettings"></param>
    /// <returns></returns>
    public static IServiceCollection AddJwtAuthentication( this IServiceCollection services, IJwtSettings jwtSettings )
    {
        ArgumentNullException.ThrowIfNull( services );
        ArgumentNullException.ThrowIfNull( jwtSettings );

        services.AddSwaggerGen( c =>
        {
            c.AddSecurityDefinition( SECURITY_DEFINITION_NAME, new OpenApiSecurityScheme
            {
                Description = @"JMT Authorization header using the Bearer scheme. \r\n\r\n
            Enter 'Bearer' [space] and then your token in the text input below. \r\n\r\n
            Example: 'Bearer 12345abcdef'",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = SECURITY_DEFINITION_NAME
            } );

            c.AddSecurityRequirement( new OpenApiSecurityRequirement()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = SECURITY_DEFINITION_NAME
                        },
                        Scheme = "oauth2",
                        Name = SECURITY_DEFINITION_NAME,
                        In = ParameterLocation.Header,
                    },
                    new List<string>()
                }
            } );
        } );

        services.AddAuthentication( auth =>
        {
            auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        } ).AddJwtBearer( o =>
        {
            var key = Encoding.UTF8.GetBytes( jwtSettings.JwtKey! );

            o.SaveToken = true;
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = Convert.ToBoolean( jwtSettings.ValidateIssuer ),
                ValidateAudience = Convert.ToBoolean( jwtSettings.ValidateAudience ),
                ValidateLifetime = Convert.ToBoolean( jwtSettings.ValidateLifetime ),
                ValidateIssuerSigningKey = Convert.ToBoolean( jwtSettings.ValidateIssuerSigningKey ),
                ValidIssuer = jwtSettings.JwtIssuer,
                ValidAudience = jwtSettings.JwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey( key )
            };
        } );

        return services;
    }
}

/// <summary>
/// En esta clase estática estaremos definiendo las extensiones requeridas en el Sistema y aplicables a MongoDB
/// </summary>
public static class ExtensionsMongo
{
    private static readonly string[] s_tags = new[] { "ready" };

    /// <summary>
    /// Con este método estamos extendiendo la funcionalidad de un objeto de tipo IServiceCollection 
    /// para adicionar una instancia "Singleton" de una Base de Datos MongoDB.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="mongoSettings"></param>
    /// <returns></returns>
    public static IServiceCollection AddMongo( this IServiceCollection services, IMongoSettings mongoSettings )
    {
        ArgumentNullException.ThrowIfNull( services );
        ArgumentNullException.ThrowIfNull( mongoSettings );

        BsonSerializer.RegisterSerializer( new GuidSerializer( BsonType.String ) );
        BsonSerializer.RegisterSerializer( new DateTimeOffsetSerializer( BsonType.String ) );

        services.AddSingleton( serviceProvider =>
        {
            var mongoClient = new MongoClient( mongoSettings.ConnectionString );
            return mongoClient.GetDatabase( mongoSettings.ServiceName );
        } );

        services.AddHealthChecks().AddMongoDb( mongoSettings.ConnectionString,
            name: "mongodb",
            timeout: TimeSpan.FromSeconds( 3 ),
            tags: s_tags );

        return services;
    }

    /// <summary>
    /// Con este método estamos extendiendo la funcionalidad de un objeto de tipo IServiceCollection 
    /// para adicionar una instancia "Singleton" de un repositorio de MongoDB.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"></param>
    /// <param name="collectionName"></param>
    /// <returns></returns>
    public static IServiceCollection AddMongoRepository<T>( this IServiceCollection services, string collectionName ) where T : IEntity
    {
        ArgumentNullException.ThrowIfNull( services );
        ArgumentNullException.ThrowIfNull( collectionName );

        services.AddSingleton<IRepository<T>>( serviceProvider =>
        {
            var database = serviceProvider.GetService<IMongoDatabase>()!;
            return new MongoRepository<T>( database, collectionName );
        } );

        return services;
    }
}