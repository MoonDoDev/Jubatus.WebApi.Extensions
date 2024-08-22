namespace Jubatus.WebApi.Extensions;
using Jubatus.WebApi.Extensions.Models;
using System.Linq.Expressions;
using MongoDB.Driver.Linq;
using MongoDB.Driver;
using FluentResults;

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public class MongoRepository<T>: IRepository<T> where T : IEntity
{
    #region private members

    private readonly IMongoCollection<T> _dbCollection;

    private readonly FilterDefinitionBuilder<T> _filterBuilder = Builders<T>.Filter;

    #endregion
    #region constructor

    /// <summary>
    /// Constructor de la clase
    /// </summary>
    /// <param name="database">Instancia "Singleton" de la MongoDB</param>
    /// <param name="collectionName">Nombre de la colección en MongoDB</param>
    public MongoRepository( IMongoDatabase database, string collectionName )
    {
        ArgumentNullException.ThrowIfNull( database );
        _dbCollection = database.GetCollection<T>( collectionName );
    }

    #endregion
    #region public members

    /// <summary>
    /// Consultamos y retornamos todos los registros de la colección que cumplan con el criterio del filtro suministrado
    /// </summary>
    /// <param name="filter">Criterio de búsqueda de los documentos en la colección.</param>
    /// <returns>Todos los registros de la colección que cumplan con el criterio de búsqueda.</returns>
    public IAsyncEnumerable<T> GetAllAsync( Expression<Func<T, bool>>? filter = null )
    {
        return _dbCollection.Find( filter ?? _filterBuilder.Empty ).ToAsyncEnumerable();
    }

    /// <summary>
    /// Consultamos y retornamos el documento de la colección que tenga el id suministrado
    /// </summary>
    /// <param name="id">Id del documento a consultar</param>
    /// <returns>La información del registro que corresponda al id suministrado.</returns>
    public async Task<Result<T>> GetAsync( Guid id )
    {
        var filter = _filterBuilder.Eq( entity => entity.Id, id );
        var record = await _dbCollection.Find( filter ).FirstOrDefaultAsync().ConfigureAwait( false );

        if( record is null )
            return Result.Fail<T>( "GetAsync( record id not found )" );

        return record;
    }

    /// <summary>
    /// Consultamos y retornamos el documento de la colección que cumpla con el criterio de búsqueda suministrado
    /// </summary>
    /// <param name="filter">Criterio de búsqueda del documento en la Colección</param>
    /// <returns>La información del registro que corresponda al criterio de búsqueda suministrado.</returns>
    public async Task<Result<T>> GetAsync( Expression<Func<T, bool>> filter )
    {
        ArgumentNullException.ThrowIfNull( filter );
        var record = await _dbCollection.Find( filter ).FirstOrDefaultAsync().ConfigureAwait( false );

        if( record is null )
            return Result.Fail<T>( "GetAsync( filter - record not found )" );

        return record;
    }

    /// <summary>
    /// Creamos un documento nuevo en la colección con los datos suministrados en el parámetro
    /// </summary>
    /// <param name="entity">Datos del documento a ingresar en la colección.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task<Result<T>> CreateAsync( T entity )
    {
        ArgumentNullException.ThrowIfNull( entity );
        await _dbCollection.InsertOneAsync( entity ).ConfigureAwait( false );
        return Result.Ok( entity );
    }

    /// <summary>
    /// Actualizamos el documento en la colección que corresponda con el Id del registro suministrado
    /// </summary>
    /// <param name="entity">Datos del documento a modificar en la colección</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task<Result<T>> UpdateAsync( T entity )
    {
        ArgumentNullException.ThrowIfNull( entity );

        var filter = _filterBuilder.Eq( existingEntity => existingEntity.Id, entity.Id );
        var result = await _dbCollection.ReplaceOneAsync( filter, entity, new ReplaceOptions { IsUpsert = false } ).ConfigureAwait( false );

        if( result.ModifiedCount > 0 )
            return Result.Ok( entity );

        return Result.Fail<T>( "UpdateAsync( record id not found )" );
    }

    /// <summary>
    /// Eliminamos de la colección el documento que corresponda con el Id suministrado
    /// </summary>
    /// <param name="id">Identificación del documento que se desea eliminar</param>
    public async Task<Result> RemoveAsync( Guid id )
    {
        var filter = _filterBuilder.Eq( entity => entity.Id, id );
        var result = await _dbCollection.DeleteOneAsync( filter ).ConfigureAwait( false );

        if( result.DeletedCount > 0 )
            return Result.Ok();

        return Result.Fail( "RemoveAsync( record id not found )" );
    }

    #endregion
}