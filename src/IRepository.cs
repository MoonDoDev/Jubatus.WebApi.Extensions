namespace Jubatus.WebApi.Extensions;
using Jubatus.WebApi.Extensions.Models;
using System.Linq.Expressions;
using FluentResults;

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IRepository<T> where T : IEntity
{
    /// <summary>
    /// Creamos un documento nuevo en la colección con los datos suministrados en el parámetro
    /// </summary>
    /// <param name="entity">Datos del documento a ingresar en la colección.</param>
    /// <exception cref="ArgumentNullException"></exception>
    Task<Result<T>> CreateAsync( T entity );

    /// <summary>
    /// Consultamos y retornamos todos los registros de la colección que cumplan con el criterio del filtro suministrado
    /// </summary>
    /// <param name="filter">Criterio de búsqueda de los documentos en la colección.</param>
    /// <returns>Todos los registros de la colección que cumplan con el criterio de búsqueda.</returns>
    IAsyncEnumerable<T> GetAllAsync( Expression<Func<T, bool>>? filter = null );

    /// <summary>
    /// Consultamos y retornamos el documento de la colección que tenga el id suministrado
    /// </summary>
    /// <param name="id">Id del documento a consultar</param>
    /// <returns>La información del registro que corresponda al id suministrado.</returns>
    Task<Result<T>> GetAsync( Guid id );

    /// <summary>
    /// Consultamos y retornamos el documento de la colección que cumpla con el criterio de búsqueda suministrado
    /// </summary>
    /// <param name="filter">Criterio de búsqueda del documento en la Colección</param>
    /// <returns>La información del registro que corresponda al criterio de búsqueda suministrado.</returns>
    Task<Result<T>> GetAsync( Expression<Func<T, bool>> filter );

    /// <summary>
    /// Eliminamos de la colección el documento que corresponda con el Id suministrado
    /// </summary>
    /// <param name="id">Identificación del documento que se desea eliminar</param>
    Task<Result> RemoveAsync( Guid id );

    /// <summary>
    /// Creamos un documento nuevo en la colección con los datos suministrados en el parámetro
    /// </summary>
    /// <param name="entity">Datos del documento a ingresar en la colección.</param>
    /// <exception cref="ArgumentNullException"></exception>
    Task<Result<T>> UpdateAsync( T entity );
}