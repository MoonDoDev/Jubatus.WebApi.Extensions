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

/// <summary>
/// 
/// </summary>
/// <param name="appBuilder"></param>
public sealed class WebApiConfig
{
    #region private fields

    private static readonly string[] s_tags = ["ready"];
    private const string SECURITY_DEFINITION_NAME = "Bearer";
    private readonly WebApplicationBuilder _appBuilder;
    private bool _rateLimiterCreated;

    #endregion
    #region primary constructor

    /// <summary>
    /// 
    /// </summary>
    /// <param name="appBuilder"></param>
    public WebApiConfig( WebApplicationBuilder appBuilder )
    {
        ArgumentNullException.ThrowIfNull( appBuilder );
        _appBuilder = appBuilder;

        /* Cargamos la configuración de WebApi Caller */
        _appBuilder.Configuration
            .AddJsonFile( "appsettings.json", optional: true, reloadOnChange: true )
            .AddJsonFile( $"appsettings.{_appBuilder.Environment}.json", true, true );

        /* Para evitar que el comiplador nos elimine el sufijo "Async" de los métodos */
        _appBuilder.Services.AddControllers( options => options.SuppressAsyncSuffixInActionNames = false );
        _appBuilder.Services.AddHealthChecks();
    }

    #endregion
    #region public methods

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="addMongoDbHealthCheck"></param>
    /// <param name="mongoDbHealthCheckTimeout"></param>
    /// <param name="configSectionName"></param>
    /// <returns></returns>
    public WebApiConfig AddMongoDbExtensions<T>(
        bool addMongoDbHealthCheck = true,
        double mongoDbHealthCheckTimeout = 3,
        string configSectionName = "MongoDbSettings" ) where T : IEntity
    {
        BsonSerializer.RegisterSerializer( new GuidSerializer( BsonType.String ) );
        BsonSerializer.RegisterSerializer( new DateTimeOffsetSerializer( BsonType.String ) );

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
    /// 
    /// </summary>
    /// <param name="configSectionName"></param>
    /// <returns></returns>
    public WebApiConfig AddBearerJwtExtensions(
        string configSectionName = "JwtSettings" )
    {
        var jwtOptions = new JwtSettings();
        _appBuilder.Configuration.GetSection( configSectionName ).Bind( jwtOptions );

        _appBuilder.Services.AddSwaggerGen( c =>
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
    /// 
    /// </summary>
    /// <param name="permitLimit"></param>
    /// <param name="secondsTimeout"></param>
    /// <param name="processingOrder"></param>
    /// <param name="queueLimit"></param>
    /// <returns></returns>
    public WebApiConfig AddFixedRateLimiter(
        int permitLimit = 10,
        double secondsTimeout = 5,
        QueueProcessingOrder processingOrder = QueueProcessingOrder.OldestFirst,
        int queueLimit = 2 )
    {
        _appBuilder.Services.AddRateLimiter( rateLimiterOptions =>
            rateLimiterOptions.AddFixedWindowLimiter( policyName: "fixed", options =>
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
    /// 
    /// </summary>
    /// <param name="majorVer"></param>
    /// <param name="minorVer"></param>
    /// <param name="status"></param>
    /// <returns></returns>
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
    /// 
    /// </summary>
    /// <param name="serviceHealthCheckEndpoint"></param>
    /// <param name="mongoHealthCheckEndpoint"></param>
    /// <returns></returns>
    public WebApplication BuildWebApp( string? serviceHealthCheckEndpoint = null, string? mongoHealthCheckEndpoint = null )
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
                Predicate = ( check ) => check.Tags.Contains( "ready" ),
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
            app.MapDefaultControllerRoute().RequireRateLimiting( "fixed" );
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }

    #endregion
}