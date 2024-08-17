# Jubatus.WebApi.Extensions - Colección de extensiones para proyectos de .NET 8.

Este es un paquete de libre distribución, que inicialmente fue desarrollado para uso en proyectos personales, y para ser muy honesto, me motivé a subirlo a [**NuGet Gallery**](https://www.nuget.org/), inicialmente para facilitar el despliegue de dichos proyectos a través de contenedores de [**Docker**](https://www.docker.com); pero más allá de eso, espero poder hacer un pequeño aporte a la comunidad, o ahorrarle un poco de trabajo a alguien.

Este paquete es una colección de funcionalidades, que por ahora permitirá, a través de *Extensions* e *Interfaces* la inclusión de ciertas características a una Colección de Servicios `IServiceCollection` del *namespace* `Microsoft.Extensions.DependencyInjection`. A continuación se describen los detalles:

```
  Jubatus.WebApi.Extensions \
  | MongoDB \
    | MongoRepository  // Implementación del CRUD.
  | Settings \
    | IJwtSettings     // Interface para la configuración del Bearer Token.
    | IMongoSettings   // Interface para la conexión a MongoDB.
  | Constants          // Definición de constantes.
  | Extensions         // Extensiones de IServiceCollection.
  | IEntity            // Interface para crear los Modelos y DTO's.
  | IRepository        // Interface para la implementación del CRUD.
  | Logger             // Métodos para escribir al Logger (por ahora solo Consola)
```

## ¿Cómo adicionar el paquete al proyecto y usarlo?
Abrimos una Terminal en nuestro ambiente de desarrollo de Visual Studio, y nos ubicamos en el directorio donde se encuentra el archivo del proyecto '*.csproj', y allí ejecutamos el siguiente comando:

```
dotnet add package Jubatus.Common [--version 1.0.16]
```

### ¿Cómo creo una instancia *Singleton* de [**MongoDB**](https://www.mongodb.com)?
- [x]  Incluimos el *namespace* **Jubatus.Common**.

```
using Jubatus.Common;
```

- [x]  Hacemos el llamado del método `AddMongo` de la Colección de Servicios `IServiceCollection` del *namespace* `Microsoft.Extensions.DependencyInjection`, suministrando una clase que implemente la interface `IMongoSettings`. De los parámetros suministrados, tomamos el valor de la propiedad `ConnectionString` para configurar la instancia, y la propiedad `ServiceName` para asignar el nombre a la Base de Datos. Adicionalmente cuando se ejecuta el método `AddMongo`, se estaría vinculando a la misma Colección de Servicios, el *HealthCheck* que permitirá la validación de la disponibilidad de la instancia de MongoDB (Faltaría el mapeo de dicho proceso, que ya estaría a tu cargo).

```
builder.Services.AddMongo(mongoDbSettings);
```

> [!WARNING]
> Este método reporta en la consola del proceso, a modo de Debug, la configuración suministrada en el parámetro (Se sugiere usar el atributo `[NotLogged]` del namespace `Destructurama.Attributed` para proteger las propiedades que no se desea visualizar).

- [x]  Para crear una colección en la BD, ejecutamos el método `AddMongoRepository` de la Colección de Servicios `IServiceCollection`, pasándole como tipo `<T>` la clase o record que implementa la interface `IEntity`, y con la cual estamos definiendo la estructura de datos que vamos a guardar en ella. En el parámetro `collectionName` le indicamos el nombre que le queremos asignar a dicha colección.

```
builder.Services.AddMongoRepository<UsersEntity>("Users");
```

- [x]  Para ejecutar el CRUD de la colección definida previamente, debemos apoyarnos de la *DI (Dependency Injection)*, para obtener en el constructor de nuestro *Controller* la instancia *Singleton* del repositorio, y ya con éste, hacer el llamado de los métodos asíncronos `GetAllAsync()`, `GetAsync()`, `CreateAsync()`, `UpdateAsync()`, y `RemoveAsync()`.

```
public UsersController(IRepository<UsersEntity> usersRepository) { ... }
```

> [!WARNING]
> Los métodos del CRUD reportan en la consola del proceso, a modo de Debug, los datos guardados en la colección (Se sugiere usar el atributo `[NotLogged]` del namespace `Destructurama.Attributed` para proteger las propiedades que no se desea visualizar).

### ¿Cómo puedo enviar mensajes a la Consola del Proceso?
- [x]  Incluimos el *namespace* **Jubatus.Common**.

```
using Jubatus.Common;
```

- [x]  Hacemos el llamado del método `Logger.GetLogger()`, indicándole como parámetro el nivel mínimo de visibilidad, el cual está definido en el *enum* `LoggerMinLevel`.

```
// Se sugiere hacer el llamado con 'using' para liberar los recursos rápidamente
using var log = Logger.GetLogger(LoggerMinLevel.Debug);
```

### ¿Cómo puedo configurar en el Servicio la Autenticación con Bearer Tokens (JWT)?
- [x]  Incluimos el *namespace* **Jubatus.Common**.

```
using Jubatus.Common;
```

- [x]  Hacemos el llamado del método `AddJwtAuthentication` de la Colección de Servicios `IServiceCollection`, suministrando una clase que implemente la interface `IJwtSettings`. Con esta información se vincula a la colección de Servicios, el llamado a *SwaggerGen()* y a *Authentication()*.

> [!WARNING]
> Este método reporta igualmente a la consola del proceso, a modo de Debug, la configuración suministrada (Se sugiere tener en cuenta la misma recomendación mencionada anteriormente para proteger la información).

## Dependencias

```
"AspNetCore.HealthChecks.MongoDb" Version="8.0.1"
"Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.8"
"Microsoft.Extensions.Configuration" Version="8.0.0"
"Microsoft.Extensions.Configuration.Binder" Version="8.0.2"
"Microsoft.Extensions.DependencyInjection" Version="8.0.0"
"Microsoft.AspNetCore.OpenApi" Version="8.0.4"
"MongoDB.Driver" Version="2.25.0"
"Serilog.AspNetCore" Version="8.0.2"
"Destructurama.Attributed" Version="4.0.0"
"Swashbuckle.AspNetCore" Version="6.7.0"
```

---------

[**YouTube**](https://www.youtube.com/@hectorgomez-backend-dev/featured) -- 
[**LinkedIn**](https://www.linkedin.com/in/hectorgomez-backend-dev/) -- 
[**GitHub**](https://github.com/MoonDoDev/JubatusCommon)
