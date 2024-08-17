using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace Jubatus.WebApi.Extensions.MongoDB;

public class MongoRepository<T>: IRepository<T> where T : IEntity
{
    #region private members

    private readonly IMongoCollection<T> _dbCollection;

    private readonly FilterDefinitionBuilder<T> _filterBuilder = Builders<T>.Filter;

    #endregion
    #region public readonly properties

    /// <summary>
    /// 
    /// </summary>
    public int ResultCode { get; private set; } = int.MinValue;

    /// <summary>
    /// 
    /// </summary>
    public string ResultMessage { get; private set; } = string.Empty;

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
    /// Consultamos y retornamos todos los registros de la colección
    /// </summary>
    /// <returns>Todos los registros de la colección</returns>
    public async Task<IReadOnlyCollection<T>> GetAllAsync()
    {
        try
        {
            var records = await _dbCollection.Find( _filterBuilder.Empty ).ToListAsync().ConfigureAwait( false );

            ResultMessage = $"GetAllAsync( returns <{records.Count}> records )";
            ResultCode = StatusCodes.Status200OK;
            return records;
        }
        catch( System.Exception ex )
        {
            ResultMessage = $"GetAllAsync( thrown an exception: <{ex}> )";
            ResultCode = StatusCodes.Status500InternalServerError;
            throw;
        }
    }

    /// <summary>
    /// Consultamos y retornamos todos los registros de la colección que cumplan con el criterio del filtro suministrado
    /// </summary>
    /// <param name="filter">Criterio de búsqueda de los documentos en la colección.</param>
    /// <returns>Todos los registros de la colección que cumplan con el criterio de búsqueda.</returns>
    public async Task<IReadOnlyCollection<T>> GetAllAsync( Expression<Func<T, bool>> filter )
    {
        try
        {
            var records = await _dbCollection.Find( filter ).ToListAsync().ConfigureAwait( false );

            ResultMessage = $"GetAllAsync( returns <{records.Count}> records )";
            ResultCode = StatusCodes.Status200OK;
            return records;
        }
        catch( System.Exception ex )
        {
            ResultMessage = $"GetAllAsync( thrown an exception: <{ex}> )";
            ResultCode = StatusCodes.Status500InternalServerError;
            throw;
        }
    }

    /// <summary>
    /// Consultamos y retornamos el documento de la colección que tenga el id suministrado
    /// </summary>
    /// <param name="id">Id del documento a consultar</param>
    /// <returns>La información del registro que corresponda al id suministrado.</returns>
    public async Task<T> GetAsync( Guid id )
    {
        try
        {
            var filter = _filterBuilder.Eq( entity => entity.Id, id );
            var record = await _dbCollection.Find( filter ).FirstOrDefaultAsync().ConfigureAwait( false );

            if( EqualityComparer<T>.Default.Equals( record, default ) )
            {
                ResultMessage = $"GetAsync( returns: record with id <{id}> doesn't exist )";
                ResultCode = StatusCodes.Status404NotFound;
            }
            else
            {
                ResultMessage = $"GetAsync( returns: successful answer )";
                ResultCode = StatusCodes.Status200OK;
            }

            return record!;
        }
        catch( System.Exception ex )
        {
            ResultMessage = $"GetAsync( thrown an exception: <{ex}> )";
            ResultCode = StatusCodes.Status500InternalServerError;
            throw;
        }
    }

    /// <summary>
    /// Consultamos y retornamos el documento de la colección que cumpla con el criterio de búsqueda suministrado
    /// </summary>
    /// <param name="filter">Criterio de búsqueda del documento en la Colección</param>
    /// <returns>La información del registro que corresponda al criterio de búsqueda suministrado.</returns>
    public async Task<T> GetAsync( Expression<Func<T, bool>> filter )
    {
        try
        {
            var record = await _dbCollection.Find( filter ).FirstOrDefaultAsync().ConfigureAwait( false );

            if( EqualityComparer<T>.Default.Equals( record, default ) )
            {
                ResultMessage = $"GetAsync( returns: record with that criteria doesn't exist )";
                ResultCode = StatusCodes.Status404NotFound;
            }
            else
            {
                ResultMessage = $"GetAsync( returns: successful answer )";
                ResultCode = StatusCodes.Status200OK;
            }

            return record!;
        }
        catch( System.Exception ex )
        {
            ResultMessage = $"GetAsync( thrown an exception: <{ex}> )";
            ResultCode = StatusCodes.Status500InternalServerError;
            throw;
        }
    }

    /// <summary>
    /// Creamos un documento nuevo en la colección con los datos suministrados en el parámetro
    /// </summary>
    /// <param name="entity">Datos del documento a ingresar en la colección.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task CreateAsync( T entity )
    {
        ArgumentNullException.ThrowIfNull( entity );

        try
        {
            await _dbCollection.InsertOneAsync( entity ).ConfigureAwait( false );

            ResultMessage = $"CreateAsync( returns: record has been created )";
            ResultCode = StatusCodes.Status201Created;
        }
        catch( System.Exception ex )
        {
            ResultMessage = $"CreateAsync( thrown an exception: <{ex}> )";
            ResultCode = StatusCodes.Status500InternalServerError;
            throw;
        }
    }

    /// <summary>
    /// Actualizamos el documento en la colección que corresponda con el Id del registro suministrado
    /// </summary>
    /// <param name="entity">Datos del documento a modificar en la colección</param>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task UpdateAsync( T entity )
    {
        ArgumentNullException.ThrowIfNull( entity );

        try
        {
            var filter = _filterBuilder.Eq( existingEntity => existingEntity.Id, entity.Id );
            var result = await _dbCollection.ReplaceOneAsync( filter, entity, new ReplaceOptions { IsUpsert = false } ).ConfigureAwait( false );

            if( result.ModifiedCount > 0 )
            {
                ResultMessage = $"UpdateAsync( returns: record has been updated )";
                ResultCode = StatusCodes.Status204NoContent;
            }
            else
            {
                ResultMessage = $"UpdateAsync( returns: record to update doesn't exist )";
                ResultCode = StatusCodes.Status404NotFound;
            }
        }
        catch( System.Exception ex )
        {
            ResultMessage = $"UpdateAsync( thrown an exception: <{ex}> )";
            ResultCode = StatusCodes.Status500InternalServerError;
            throw;
        }
    }

    /// <summary>
    /// Eliminamos de la colección el documento que corresponda con el Id suministrado
    /// </summary>
    /// <param name="id">Identificación del documento que se desea eliminar</param>
    public async Task RemoveAsync( Guid id )
    {
        try
        {
            var filter = _filterBuilder.Eq( entity => entity.Id, id );
            var result = await _dbCollection.DeleteOneAsync( filter ).ConfigureAwait( false );

            if( result.DeletedCount > 0 )
            {
                ResultMessage = $"RemoveAsync( returns: record has been deleted )";
                ResultCode = StatusCodes.Status204NoContent;
            }
            else
            {
                ResultMessage = $"RemoveAsync( returns: record to delete doesn't exist )";
                ResultCode = StatusCodes.Status404NotFound;
            }
        }
        catch( System.Exception ex )
        {
            ResultMessage = $"RemoveAsync( thrown an exception: <{ex}> )";
            ResultCode = StatusCodes.Status500InternalServerError;
            throw;
        }
    }

    #endregion
}