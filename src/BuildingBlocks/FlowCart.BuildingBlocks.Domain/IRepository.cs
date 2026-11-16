using System.Linq.Expressions;

namespace FlowCart.BuildingBlocks.Domain;

/// <summary>
/// Generic repository interface abstracting query and mutation persistence logic for aggregate roots.
/// </summary>
/// <typeparam name="T">The aggregate root type.</typeparam>
/// <typeparam name="TId">The aggregate root primary key identifier type.</typeparam>
public interface IRepository<T, in TId>
    where T : Entity<TId>, IAggregateRoot
    where TId : notnull
{
    /// <summary>
    /// Retrieves an aggregate root by its unique identifier.
    /// </summary>
    Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all aggregate roots.
    /// </summary>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds aggregate roots matching the specified predicate expression.
    /// </summary>
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated slice of aggregate roots matching the optional filter and ordering.
    /// </summary>
    Task<(IReadOnlyList<T> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether any aggregate root matches the specified predicate.
    /// </summary>
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts aggregate roots matching an optional predicate.
    /// </summary>
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enqueues a new aggregate root for insertion.
    /// </summary>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an existing aggregate root as modified.
    /// </summary>
    void Update(T entity);

    /// <summary>
    /// Enqueues an aggregate root for deletion.
    /// </summary>
    void Remove(T entity);
}
