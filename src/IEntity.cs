using System;

namespace Jubatus.WebApi.Extensions;

/// <summary>
/// Interface que define la base para del objeto que se usará para gestionar los datos de la Entidad
/// </summary>
public interface IEntity
{
    Guid Id { get; init; }
}
