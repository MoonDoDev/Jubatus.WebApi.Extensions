namespace Jubatus.WebApi.Extensions;

using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Jubatus.WebApi.Extensions.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Jubatus.WebApi.Extensions.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Jubatus.WebApi.Extensions.Exceptions;

public sealed class WebApiConfig
{
    #region private data

    private static readonly string[] s_tags = ["ready"];
    private const string POLICY_NAME = "fixed";
    private bool _rateLimiterCreated;
    private readonly WebApplicationBuilder _appBuilder;

    #endregion
    #region primary constructor

    public WebApiConfig( WebApplicationBuilder appBuilder )
    {
        ArgumentNullException.ThrowIfNull( appBuilder );
        _appBuilder = appBuilder;

        /* Cargamos la configuración de WebApi Caller */
        _appBuilder.Configuration
            .AddJsonFile( "appsettings.json", optional: true, reloadOnChange: true )
            .AddJsonFile( $"appsettings.{_appBuilder.Environment}.json", true, true );

        /* Para evitar que el compilador nos elimine el sufijo "Async" de los métodos */
        _appBuilder.Services.AddControllers( options => options.SuppressAsyncSuffixInActionNames = false );
        _appBuilder.Services.AddHealthChecks();

        /* Adicionamos al ServiceCollection los manejadores de Excepciones */
        _appBuilder.Services.AddExceptionHandler<GeneralExceptionHandler>();
        _appBuilder.Services.AddProblemDetails();
    }

    #endregion
    #region public methods

    /// <summary>
    /// Método con el que adicionamos a ServiceCollection las instancias Singleton de la BD de MongoDB y una colección con la estructura 
    /// basada en el tipo <T>. Opcionalmente se puede agregar un "HealthCheck" para validar la disponibilidad de la BD de MongoDB.
    /// </summary>
    /// <typeparam name="T">Clase base para crear la colección en la BD de MongoDB.</typeparam>
    /// <param name="addMongoDbHealthCheck">Indicamos TRUE si se desea adicionar el HealthCheck hacia la BD de MongoDB, en caso contrario 
    /// indicamos FALSE. Valor por defecto TRUE.</param>
    /// <param name="mongoDbHealthCheckTimeout">Indicamos el tiempo máximo que esperaremos en segundos, por la respuesta del HealthCheck
    /// hacia la BD de MongoDB. Por defecto su valor será de 3 segundos.</param>
    /// <param name="configSectionName">Nombre de la sección en el archivo de configuración "appsettings.json" que contiene los parámetros
    /// de configuración para la conexión hacia la instancia de MongoDB. Por defecto su valor es "MongoDbSettings".</param>
    /// <returns>Retornamos la instancia actualizada de esta clase.</returns>
    public WebApiConfig AddMongoDbExtensions<T>(
        bool addMongoDbHealthCheck = true,
        double mongoDbHealthCheckTimeout = 3,
        string configSectionName = "MongoDbSettings" ) where T : IEntity
    {
        // Registramos los Serializadores de Guid y DatetimeOffset para que sean guardados en la BD de Mongo como un String
        BsonSerializer.RegisterSerializer( new GuidSerializer( BsonType.String ) );
        BsonSerializer.RegisterSerializer( new DateTimeOffsetSerializer( BsonType.String ) );

        // Consultamos los parámetros de configuración requerida con "Options Patterns"
        var mongoOptions = new MongoDbSettings();
        _appBuilder.Configuration.GetSection( configSectionName ).Bind( mongoOptions );

        _appBuilder.Services.AddSingleton( serviceProvider =>
        {
            var mongoClient = new MongoClient( mongoOptions.ConnectionString! );
            return mongoClient.GetDatabase( mongoOptions.ServiceName );
        } );

        if( addMongoDbHealthCheck )
        {
            _appBuilder.Services.AddHealthChecks()
                .AddMongoDb( mongoOptions.ConnectionString!,
                    name: "mongodb",
                    timeout: TimeSpan.FromSeconds( mongoDbHealthCheckTimeout ),
                    tags: s_tags );
        }

        _appBuilder.Services.AddSingleton<IRepository<T>>( serviceProvider =>
        {
            var database = serviceProvider.GetService<IMongoDatabase>()!;
            return new MongoRepository<T>( database, mongoOptions.CollectionName! );
        } );

        return this;
    }

    /// <summary>
    /// Método que nos permitirá adicionar a ServiceCollection la Autenticación con JSON Web Token (Bearer Token).
    /// </summary>
    /// <param name="configSectionName">Nombre de la sección en el archivo de configuración "appsettings.json" que contiene 
    /// los parámetros para la configuración de la Autenticación con JWT. Por defecto se indica el valor "JwtSettings".</param>
    /// <returns>Retornamos la instancia actualizada de esta clase.</returns>
    public WebApiConfig AddBearerJwtExtensions(
        string configSectionName = "JwtSettings" )
    {
        var jwtOptions = new JwtSettings();
        _appBuilder.Configuration.GetSection( configSectionName ).Bind( jwtOptions );

        _appBuilder.Services.AddSwaggerGen( c =>
        {
            c.AddSecurityDefinition( JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
            {
                Description = "JMT Authorization header - Enter 'Bearer' space and [Token]",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = JwtBearerDefaults.AuthenticationScheme
            } );

            c.AddSecurityRequirement( new OpenApiSecurityRequirement()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = JwtBearerDefaults.AuthenticationScheme
                        },
                        Scheme = "oauth2",
                        Name = JwtBearerDefaults.AuthenticationScheme,
                        In = ParameterLocation.Header,
                    },
                    new List<string>()
                }
            } );
        } );

        _appBuilder.Services.AddAuthentication( auth =>
        {
            auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        } ).AddJwtBearer( o =>
        {
            var key = Encoding.UTF8.GetBytes( jwtOptions.JwtKey! );

            o.SaveToken = true;
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = Convert.ToBoolean( jwtOptions.ValidateIssuer ),
                ValidateAudience = Convert.ToBoolean( jwtOptions.ValidateAudience ),
                ValidateLifetime = Convert.ToBoolean( jwtOptions.ValidateLifetime ),
                ValidateIssuerSigningKey = Convert.ToBoolean( jwtOptions.ValidateIssuerSigningKey ),
                ValidIssuer = jwtOptions.JwtIssuer,
                ValidAudience = jwtOptions.JwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey( key )
            };
        } );

        return this;
    }

    /// <summary>
    /// Método que nos permitirá adicionar/configurar en ServiceCollection la funcionalidad básica 
    /// de "RateLimiter" con la política "fixed".
    /// </summary>
    /// <param name="permitLimit">Indicamos el número máximo de "Request" simultáneos permitidas. 
    /// El valor por defecto es 10.</param>
    /// <param name="secondsTimeout">Indicamos el tiempo máximo de atención del "Request" en segundos.
    /// El valor por defecto es 5.</param>
    /// <param name="processingOrder">Indicamos el orden en el que se atenderan los "Request" en la cola.
    /// El valor por defecto es QueueProcessingOrder.OldestFirst.</param>
    /// <param name="queueLimit">Indicamos la cantidad máxima de "Request" en cola.
    /// Por defecto se define el valor 2.</param>
    /// <returns>Retornamos la instancia actualizada de esta clase.</returns>
    public WebApiConfig AddFixedRateLimiter(
        int permitLimit = 10,
        double secondsTimeout = 5,
        QueueProcessingOrder processingOrder = QueueProcessingOrder.OldestFirst,
        int queueLimit = 2 )
    {
        _appBuilder.Services.AddRateLimiter( rateLimiterOptions =>
            rateLimiterOptions.AddFixedWindowLimiter( policyName: POLICY_NAME, options =>
            {
                options.PermitLimit = permitLimit;                          // A maximum of 10 requests
                options.Window = TimeSpan.FromSeconds( secondsTimeout );    // Per 5 seconds window.
                options.QueueProcessingOrder = processingOrder;             // Behaviour when not enough resources can be leased (Process oldest requests first).
                options.QueueLimit = queueLimit;                            // Maximum cumulative permit count of queued acquisition requests.
            } )
        );

        _rateLimiterCreated = true;
        return this;
    }

    /// <summary>
    /// Método que permite adicionar/configurar en ServiceCollection la funcionalidad para el 
    /// versionamiento de las APIs y sus Endpoints a través de las URLs y los Headers.
    /// </summary>
    /// <param name="majorVer">Indicamos la versión mayor. Valor por defecto 1.</param>
    /// <param name="minorVer">Indicamos la versión menor. Valor por defecto null.</param>
    /// <param name="status">Indicamos el estado de la versión. Valor por defecto null.</param>
    /// <returns>Retornamos la instancia actualizada de esta clase.</returns>
    public WebApiConfig AddUrlAndHeaderApiVersioning(
        int majorVer = 1,
        int? minorVer = null,
        string? status = null )
    {
        _appBuilder.Services.AddApiVersioning( options =>
        {
            options.DefaultApiVersion = new ApiVersion( majorVer, minorVer, status );
            options.ReportApiVersions = true;
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader( "X-Api-Version" ) );
        } ).AddApiExplorer( options =>
        {
            options.GroupNameFormat = "'v'V";
            options.SubstituteApiVersionInUrl = true;
        } );

        return this;
    }

    /// <summary>
    /// Método con el que construiremos un WebApplication con los mapeos necesarios, según 
    /// las funcionalidades adicionadas al ServiceCollection con los métodos anteriores.
    /// </summary>
    /// <param name="serviceHealthCheckEndpoint">Indicamos el Endpoint a utilizar para exponer 
    /// el "HealthCheck" del Servicio. El valor por defecto es null.</param>
    /// <param name="mongoHealthCheckEndpoint">Indicamos el Endpoint a utilizar para exponer el
    /// "HealthCheck" de la BD de MongoDB. El valor por defecto es null.</param>
    /// <returns>Retornamos una WebApplication configurada.</returns>
    public WebApplication BuildWebApp(
        string? serviceHealthCheckEndpoint = null,
        string? mongoHealthCheckEndpoint = null )
    {
        var app = _appBuilder.Build();

        if( serviceHealthCheckEndpoint is not null )
        {
            app.MapHealthChecks( serviceHealthCheckEndpoint, new HealthCheckOptions
            {
                Predicate = ( _ ) => false
            } );
        }

        if( mongoHealthCheckEndpoint is not null )
        {
            app.MapHealthChecks( mongoHealthCheckEndpoint, new HealthCheckOptions
            {
                Predicate = ( check ) => check.Tags.Contains( s_tags[0] ),
                ResponseWriter = async ( context, report ) =>
                {
                    var result = JsonSerializer.Serialize( new
                    {
                        status = report.Status.ToString(),
                        checks = report.Entries.Select( entry => new
                        {
                            name = entry.Key,
                            status = entry.Value.Status.ToString(),
                            exception = entry.Value.Exception != null ? entry.Value.Exception.Message : "none",
                            duration = entry.Value.Duration.ToString(),
                        } )
                    } );

                    context.Response.ContentType = MediaTypeNames.Application.Json;
                    await context.Response.WriteAsync( result ).ConfigureAwait( false );
                }
            } );
        }

        if( _rateLimiterCreated )
        {
            app.UseRateLimiter();
            app.MapDefaultControllerRoute().RequireRateLimiting( POLICY_NAME );
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.UseExceptionHandler();

        return app;
    }

    #endregion
}