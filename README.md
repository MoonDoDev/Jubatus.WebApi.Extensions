# Jubatus.WebApi.Extensions - Colección de extensiones para WebApis de .NET 8.

Este es un paquete de libre distribución, que inicialmente fue desarrollado para uso en proyectos personales, y para ser muy honesto, me motivé a subirlo a [**NuGet Gallery**](https://www.nuget.org/), inicialmente para facilitar el despliegue de dichos proyectos a través de contenedores de [**Docker**](https://www.docker.com); pero más allá de eso, espero poder hacer un pequeño aporte a la comunidad, y ahorrarles un poco de trabajo.

Este paquete es una colección de funcionalidades, que por ahora permitirá, a través de *Extensions*, *Interfaces* y *Dependency Injection* la inclusión de ciertas características a un proyecto de tipo WebApi de .NET 8. A continuación se describen las características:

```
  Jubatus.WebApi.Extensions \
  | Models \
    | CypherModel      // Modelo para el manejo de usuarios y su contraseña.
    | ICypherModel     // Interface para definir el modelo de usuarios.
    | IEntity          // Interface para crear los Modelos y DTO's.
    | TokensModel      // Modelo para el manejo de Bearer Tokens.
  | Settings \
    | IJwtSettings     // Interface para la configuración del Bearer Token.
    | IMongoDbSettings // Interface para la configuración de MongoDB.
    | JwtSettings      // Clase con la estructura de configuración del JWT.
    | MongoDbSettings  // Clase con la estructura de configuración del MongoDB
  | IRepository        // Interface para la implementación del CRUD.
  | MongoRepository    // Implementación del CRUD con MongoDB.
  | Toolbox            // Implementación de métodos utilitarios.
  | WebApiConfig       // Clase principal que crea y asigna las extensiones.
```

## ¿Cómo adicionar el paquete al proyecto y usarlo?
Abrimos una Terminal en nuestro ambiente de desarrollo de Visual Studio, y nos ubicamos en el directorio donde se encuentra el archivo del proyecto '*.csproj', y allí ejecutamos el siguiente comando:

```
dotnet add package Jubatus.WebApi.Extensions --version 1.2.29
```

### ¿Cómo creo una instancia *Singleton* de [**MongoDB**](https://www.mongodb.com) y una colección en ella para almacenar mis datos?
- [x]  Incluimos el *namespace* **Jubatus.WebApi.Extensions** en el *Program.cs*.

```
using Jubatus.WebApi.Extensions;
```

- [x]  A continuación creamos una instancia de `WebApiConfig()`, y hacemos el llamado al método `AddMongoDbExtensions<T>()`, indicando el modelo de datos que vamos a utilizar en la colección de MongoDB (Este modelo debe implementar la interface `IEntity`).

```
[...]
var builder = WebApplication.CreateBuilder( args );
[...]
var webApiMgr = new WebApiConfig( builder )
    .AddMongoDbExtensions<UsersEntity>();
```

> [!IMPORTANT]
> El método `AddMongoDbExtensions<T>()` tiene varios parámetros, pero todos con valores por defecto, y son los siguientes:
> - addServiceHealthCheck: De tipo `bool`, y nos sirve para indicar si se desea crear/asignar el HealthCheck para el Servicio base (Por defecto está en TRUE).
> - addMongoDbHealthCheck: De tipo `bool`, y nos sirve para indicar se se desea crear/asignar el HealthCheck para la BD de MongoDB (Por defecto está en TRUE).
> - mongoDbHealthCheckTimeout: De tipo `double`, y nos sirve para configurar el tiempo de espera para la respuesta del HealthCheck de MongoDB (Por defecto está en 3 segundos).
> - configSectionName: De tipo `string`, y lo utilizaremos para indicar el nombre de tiene la sección en el archivo de configuración "appsettings.json", la cual contiene los parámetros para la conexión a MongoDB (Esta sección debe contener mínimamente los parámetros indicados en la interface `IMongoDbSettings`, y por defecto tiene el valor "MongoDbSettings").

- [x]  Haciendo el llamado al método `AddMongoDbExtensions<T>()` estamos creando también la colección para almacenar los datos, tomando para su nombre el valor de la llave *"CollectionName"* de la sección *"MongoDbSettings"* del archivo de configuración *"appsettings.json"*. Esta colección tendrá la estructura definida en la clase que se pase como parámetro `<T>` en el método, que para el ejemplo de arriba, sería `UserEntity` (Esta clase debe implementar la interface `IEntity`).

- [x]  Para ejecutar el CRUD de la colección definida previamente, debemos apoyarnos de la **DI (Dependency Injection)**, para obtener en el constructor de nuestro Controlador, la instancia *Singleton* del repositorio, y ya con éste, hacer el llamado a los métodos asíncronos `GetAllAsync()`, `GetAsync()`, `CreateAsync()`, `UpdateAsync()`, y `RemoveAsync()`.

```
public UsersController( IRepository<UsersEntity> usersRepository ): ControllerBase { ... }
```

### ¿Cómo puedo configurar en el Servicio la Autenticación y Autorización con [**Bearer Tokens - JWT**](https://jwt.io/introduction)?
- [x]  Incluimos el *namespace* **Jubatus.WebApi.Extensions** en el *Program.cs*.

```
using Jubatus.WebApi.Extensions;
```

- [x]  A continuación creamos una instancia de `WebApiConfig()`, y hacemos el llamado al método `AddBearerJwtExtensions()`.

```
[...]
var builder = WebApplication.CreateBuilder( args );
[...]
var webApiMgr = new WebApiConfig( builder )
    .AddBearerJwtExtensions();
```

> [!IMPORTANT]
> El método `AddBearerJwtExtensions()` tiene un parámetro, el cual tiene un valor por defecto, y es el siguiente:
> - configSectionName: De tipo `string`, y lo utilizaremos para indicar el nombre que tiene la sección en el archivo de configuración *"appsettings.json"*, en la cual están los parámetros para el Bearer JWT (Esta sección debe contener mínimamente los parámetros indicados en la interface `IJwtSettings`, y por defecto tiene el valor "JwtSettings").

- [x]  Para la autenticación del usuario que está solicitando Bearer Tokens, nos podemos apoyar del método estático `GenerateBearerToken()` de la clase `Toolbox`. Este método requiere que se suministren los datos del usuario en una clase que implemente la interface `ICypherModel`, y adicional los demás parámetros.

> [!IMPORTANT]
> Para que el método `GenerateBearerToken()` retorne exitosamente un Bearer Token, es necesario que el Usuario y la Contraseña suministrados en el parámero `userData`, correspondan con los datos almacenados en la sección "JwtSettings" del archivo de configuración *"appsettings.json"*, y son los siguientes:
> - AuthUser: Usuario autorizado para solicitar Bearer Tokens.
> - AuthPass: Contraseña del usuario autorizado para solicitar Bearer Tokens (Para cifrar esta contraseña antes de guardarla, se puede apoyar de la extensión `EncryptUserPassword()`de la clase que implemente la interface `ICypherModel`).
> Se debe tener en cuenta que a cada uno de los controladores que requieran esta autorización deben tener asignado el atributo `[Authorize]` y esto aplicaría para cada uno de los Endpoints definidos allí, a menos que tengan el atributo `[AllowAnonymous]`, como lo indica el siguiente ejemplo:

```
[Authorize]
public UsersController( IRepository<UsersEntity> usersRepository ): ControllerBase
{
    ... 

    [AllowAnonymous]
    public async Task<ActionResult> AuthenticateAsync( [FromBody] AuthUserDto authUser ) { ... }
}
```

### ¿Cómo puede implementar en mi Servicio un [**RateLimiter**](https://learn.microsoft.com/en-us/dotnet/api/system.threading.ratelimiting.ratelimiter?view=aspnetcore-8.0) básico (fixed)?
- [x]  Incluimos el *namespace* **Jubatus.WebApi.Extensions** en el *Program.cs*.

```
using Jubatus.WebApi.Extensions;
```

- [x]  A continuación creamos una instancia de `WebApiConfig()`, y hacemos el llamado al método `AddFixedRateLimiter()`.

```
[...]
var builder = WebApplication.CreateBuilder( args );
[...]
var webApiMgr = new WebApiConfig( builder )
    .AddFixedRateLimiter();
```

> [!IMPORTANT]
> El método `AddFixedRateLimiter()` tiene varios parámetros con valores por defecto, y son los siguientes:
> - permitLimit: De tipo `int`, y nos sirve para indicar el número máximo de request simultáneos (Por defecto tiene el valor 10).
> - secondsTimeout: De tipo `double`, y nos sirve para indicar el tiempo máximo (en segundos) de cada request (Por defecto tiene el valor 5).
> - processingOrder: De tipo `QueueProcessingOrder`, y nos sirve indicar el orden de prioridad cuando se tiene pocos recursos en el Sistema (Por defecto tiene el valor `QueueProcessingOrder.OldestFirst`).
> - queueLimit: De tipo `int`, y lo utilizaremos para indicar el número máximo de peticiones en cola (Por defecto tiene el valor 2). 
> Para que esta característica tenga efecto en nuestras APIs, debemos incluir el atributo `[EnableRateLimiting( "fixed" )]`, en cada uno de los controladores, como se indica a continuación.

```
[EnableRateLimiting( "fixed" )]
public UsersController( IRepository<UsersEntity> usersRepository ): ControllerBase { ... }
```

### ¿Cómo le puedo implementar el [**Versionamiento a mis APIs**](https://weblogs.asp.net/ricardoperes/asp-net-core-api-versioning)?
- [x]  Incluimos el *namespace* **Jubatus.WebApi.Extensions** en el *Program.cs*.

```
using Jubatus.WebApi.Extensions;
```

- [x]  A continuación creamos una instancia de `WebApiConfig()`, y hacemos el llamado al método `AddUrlAndHeaderApiVersioning()`.

```
[...]
var builder = WebApplication.CreateBuilder( args );
[...]
var webApiMgr = new WebApiConfig( builder )
    .AddUrlAndHeaderApiVersioning();
```

> [!IMPORTANT]
> El método `AddUrlAndHeaderApiVersioning()` también tiene varios parámetros con valores por defecto, y son los siguientes:
> - majorVer: De tipo `int`, y aquí estamos definiendo la versión mayor por defecto (Por defecto tiene el valor 1).
> - minorVer: De tipo `int?`, para indicar la versión menor por defecto (Por defecto tiene el valor null).
> - status: De tipo `string?`, y aquí podemos incluir un estado adicional a la versión (Por defecto tiene el valor null).
> En el controlador de nuestra API, y en cada uno de los Endpoints de ella, debemos especificar las versiones a través de atributos, como se muestra a continuación:

```
[ApiVersion( 1.0 )]
public UsersController( IRepository<UsersEntity> usersRepository ): ControllerBase
{
    ... 

    [MapToApiVersion( 1.0 )]
    public async Task<ActionResult> AuthenticateAsync( [FromBody] AuthUserDto authUser ) { ... }
}
```

## Menciones y agradecimientos:

- [x]  Agradezco el gran apoyo y sus valiosas recomendaciones de [**Eddie Velasquez**](https://github.com/eddievelasquez), mi mentor y amigo.
- [x]  Gracias a / Thanks so much to [**Timo Vilppu**](https://github.com/vilppu) por su valioso aporte y la información suministrada sobre el tema `IAsyncEnumerable`.
- [x]  Gracias tambien a / Thanks so much to [**Michael Altmann**](https://github.com/altmann) por su aporte e información sobre `FluentResults`.
- [x]  Finalmente y no menos importante, un agradecimiento por el valioso aporte que hacen a la comunidad [**Stefan Djokic**](https://thecodeman.net/) y [**Milan Jovanović**](https://www.milanjovanovic.tech/)

## Dependencias

```
"Asp.Versioning.Http" Version="8.1.0"
"Asp.Versioning.Mvc.ApiExplorer" Version="8.1.0"
"AspNetCore.HealthChecks.MongoDb" Version="8.0.1"
"FluentResults" Version="3.16.0"
"Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.8"
"Microsoft.Extensions.Configuration" Version="8.0.0"
"Microsoft.Extensions.Configuration.Binder" Version="8.0.2"
"Microsoft.Extensions.DependencyInjection" Version="8.0.0"
"Microsoft.AspNetCore.OpenApi" Version="8.0.4"
"MongoDB.Driver" Version="2.25.0"
"MongoDB.Driver.Linq.AsyncEnumerable" Version="2.15.4"
"Serilog.AspNetCore" Version="8.0.2"
"Destructurama.Attributed" Version="4.0.0"
"Swashbuckle.AspNetCore" Version="6.7.0"
```

---------

[**YouTube**](https://www.youtube.com/@hectorgomez-backend-dev/featured) -- 
[**LinkedIn**](https://www.linkedin.com/in/hectorgomez-backend-dev/) -- 
[**GitHub**](https://github.com/MoonDoDev/JubatusCommon)
