// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using System.Reflection;
#if NET9_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif
using Cratis.Arc;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Observe;
using Cratis.Arc.Queries;
using Cratis.Strings;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore;

/// <summary>
/// Extension methods for <see cref="DbSet{TEntity}"/> to support observation.
/// </summary>
public static class DbSetObserveExtensions
{
    /// <summary>
    /// Create an observable query that will observe the DbSet for changes matching the filter criteria.
    /// </summary>
    /// <param name="dbSet"><see cref="DbSet{TEntity}"/> to extend.</param>
    /// <param name="filter">Optional filter expression.</param>
    /// <param name="configure">Optional function to configure the query (e.g., adding includes).</param>
    /// <typeparam name="TEntity">Type of entity in the DbSet.</typeparam>
    /// <returns><see cref="ISubject{T}"/> with a collection of the type for the DbSet.</returns>
    public static ISubject<IEnumerable<TEntity>> Observe<[DynamicallyAccessedMembers(EntityTypeMembers.Required)] TEntity>(
        this DbSet<TEntity> dbSet,
        Expression<Func<TEntity, bool>>? filter = null,
        Func<DbSet<TEntity>, IQueryable<TEntity>>? configure = null)
        where TEntity : class => dbSet.Observe(filter, false, configure);

    /// <summary>Observe matching entities without applying the client query context when requested.</summary>
    /// <param name="dbSet">The DbSet to observe.</param>
    /// <param name="filter">Optional filter expression.</param>
    /// <param name="ignoreQueryContext">Whether to ignore client paging, sorting, and total count updates.</param>
    /// <param name="configure">Optional function to configure the query (e.g., adding includes).</param>
    /// <typeparam name="TEntity">Type of entity in the DbSet.</typeparam>
    /// <returns>The observable entities.</returns>
    public static ISubject<IEnumerable<TEntity>> Observe<[DynamicallyAccessedMembers(EntityTypeMembers.Required)] TEntity>(
        this DbSet<TEntity> dbSet,
        Expression<Func<TEntity, bool>>? filter,
        bool ignoreQueryContext,
        Func<DbSet<TEntity>, IQueryable<TEntity>>? configure = null)
        where TEntity : class
    {
        filter ??= _ => true;
        return dbSet.ObserveCore(
            filter,
            configure,
            ignoreQueryContext,
            entities => new BehaviorSubject<IEnumerable<TEntity>>(entities),
            (entities, observable) => observable.OnNext([.. entities]));
    }

    /// <summary>Observe all entities with the query-context option before the optional configuration.</summary>
    /// <param name="dbSet">The DbSet to observe.</param>
    /// <param name="ignoreQueryContext">Whether to ignore client paging, sorting, and total count updates.</param>
    /// <param name="configure">Optional function to configure the query (e.g., adding includes).</param>
    /// <typeparam name="TEntity">Type of entity in the DbSet.</typeparam>
    /// <returns>The observable entities.</returns>
    public static ISubject<IEnumerable<TEntity>> Observe<[DynamicallyAccessedMembers(EntityTypeMembers.Required)] TEntity>(
        this DbSet<TEntity> dbSet,
        bool ignoreQueryContext,
        Func<DbSet<TEntity>, IQueryable<TEntity>>? configure = null)
        where TEntity : class => dbSet.Observe(null, ignoreQueryContext, configure);

    /// <summary>
    /// Create an observable query that will observe the DbSet for changes matching the filter criteria.
    /// </summary>
    /// <param name="dbSet"><see cref="DbSet{TEntity}"/> to extend.</param>
    /// <param name="filter">Optional filter expression.</param>
    /// <param name="configure">Optional function to configure the query (e.g., adding includes).</param>
    /// <typeparam name="TEntity">Type of entity in the DbSet.</typeparam>
    /// <returns>
    /// An <see cref="ISubject{T}"/> with a single instance of the type; emits <see langword="default"/> when
    /// no entity matches — after the observed entity is deleted, after an update moves it out of the filter,
    /// or when the initial query finds none.
    /// </returns>
    public static ISubject<TEntity> ObserveSingle<[DynamicallyAccessedMembers(EntityTypeMembers.Required)] TEntity>(
        this DbSet<TEntity> dbSet,
        Expression<Func<TEntity, bool>>? filter = null,
        Func<DbSet<TEntity>, IQueryable<TEntity>>? configure = null)
        where TEntity : class => dbSet.ObserveSingle(filter, false, configure);

    /// <summary>Observe one matching entity without applying the client query context when requested.</summary>
    /// <param name="dbSet">The DbSet to observe.</param>
    /// <param name="filter">Optional filter expression.</param>
    /// <param name="ignoreQueryContext">Whether to ignore client paging, sorting, and total count updates.</param>
    /// <param name="configure">Optional function to configure the query (e.g., adding includes).</param>
    /// <typeparam name="TEntity">Type of entity in the DbSet.</typeparam>
    /// <returns>The observable entity.</returns>
    public static ISubject<TEntity> ObserveSingle<[DynamicallyAccessedMembers(EntityTypeMembers.Required)] TEntity>(
        this DbSet<TEntity> dbSet,
        Expression<Func<TEntity, bool>>? filter,
        bool ignoreQueryContext,
        Func<DbSet<TEntity>, IQueryable<TEntity>>? configure = null)
        where TEntity : class
    {
        filter ??= _ => true;
        return dbSet.ObserveSingleCore(filter, configure, ignoreQueryContext);
    }

    /// <summary>Observe one entity with the query-context option before the optional configuration.</summary>
    /// <param name="dbSet">The DbSet to observe.</param>
    /// <param name="ignoreQueryContext">Whether to ignore client paging, sorting, and total count updates.</param>
    /// <param name="configure">Optional function to configure the query (e.g., adding includes).</param>
    /// <typeparam name="TEntity">Type of entity in the DbSet.</typeparam>
    /// <returns>The observable entity.</returns>
    public static ISubject<TEntity> ObserveSingle<[DynamicallyAccessedMembers(EntityTypeMembers.Required)] TEntity>(
        this DbSet<TEntity> dbSet,
        bool ignoreQueryContext,
        Func<DbSet<TEntity>, IQueryable<TEntity>>? configure = null)
        where TEntity : class => dbSet.ObserveSingle(null, ignoreQueryContext, configure);

    /// <summary>
    /// Create an observable query that will observe a single entity based on Id in the DbSet for changes.
    /// </summary>
    /// <param name="dbSet"><see cref="DbSet{TEntity}"/> to extend.</param>
    /// <param name="id">The identifier of the entity to observe.</param>
    /// <param name="configure">Optional function to configure the query (e.g., adding includes).</param>
    /// <typeparam name="TEntity">Type of entity in the DbSet.</typeparam>
    /// <typeparam name="TId">Type of id - key.</typeparam>
    /// <returns>
    /// An <see cref="ISubject{T}"/> with an instance of the type; emits <see langword="default"/> when no
    /// entity matches — after the observed entity is deleted, or when the initial query finds none.
    /// </returns>
    /// <exception cref="InvalidOperationException">The exception that is thrown when the entity type does not have an Id property.</exception>
    public static ISubject<TEntity> ObserveById<[DynamicallyAccessedMembers(EntityTypeMembers.Required)] TEntity, TId>(
        this DbSet<TEntity> dbSet,
        TId id,
        Func<DbSet<TEntity>, IQueryable<TEntity>>? configure = null)
        where TEntity : class => dbSet.ObserveById(id, false, configure);

    /// <summary>Observe an entity by Id without applying the client query context when requested.</summary>
    /// <param name="dbSet">The DbSet to observe.</param>
    /// <param name="id">The identifier of the entity to observe.</param>
    /// <param name="ignoreQueryContext">Whether to ignore client paging, sorting, and total count updates.</param>
    /// <param name="configure">Optional function to configure the query (e.g., adding includes).</param>
    /// <typeparam name="TEntity">Type of entity in the DbSet.</typeparam>
    /// <typeparam name="TId">Type of id - key.</typeparam>
    /// <returns>The observable entity.</returns>
    /// <exception cref="InvalidOperationException">The exception that is thrown when the entity type does not have an Id property.</exception>
    public static ISubject<TEntity> ObserveById<[DynamicallyAccessedMembers(EntityTypeMembers.Required)] TEntity, TId>(
        this DbSet<TEntity> dbSet,
        TId id,
        bool ignoreQueryContext,
        Func<DbSet<TEntity>, IQueryable<TEntity>>? configure = null)
        where TEntity : class
    {
        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var idProperty = GetClrIdProperty(dbSet.EntityType) ?? GetIdProperty(dbSet);
        var property = Expression.Call(typeof(EF), nameof(EF.Property), [idProperty.ClrType], parameter, Expression.Constant(idProperty.Name));
        var constant = Expression.Constant(id, typeof(TId));
        var equals = Expression.Equal(property, constant);
        var lambda = Expression.Lambda<Func<TEntity, bool>>(equals, parameter);

        return dbSet.ObserveSingleCore(lambda, configure, ignoreQueryContext);
    }

#if NET9_0_OR_GREATER
    /// <summary>
    /// Sorts a query without closing a generic method through reflection over dynamic code.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="query">The <see cref="IQueryable{T}"/> to sort.</param>
    /// <param name="propertyAccess">The <see cref="Expression"/> reading the property to sort on.</param>
    /// <param name="parameter">The <see cref="ParameterExpression"/> of the entity.</param>
    /// <param name="direction">The <see cref="SortDirection"/>.</param>
    /// <returns>The sorted <see cref="IQueryable{T}"/>.</returns>
    /// <exception cref="MissingQueryableSortKey">Thrown when the property type is not registered and cannot be closed without dynamic code.</exception>
    /// <exception cref="TargetInvocationException">Thrown when building the sort fails, with the failure as inner exception.</exception>
    internal static IQueryable<TEntity> OrderByWithoutDynamicCode<TEntity>(IQueryable<TEntity> query, Expression propertyAccess, ParameterExpression parameter, SortDirection direction)
        where TEntity : class
    {
        var keyType = QueryableKeyTypes.For(typeof(TEntity), propertyAccess.Type);

        try
        {
            return keyType.OrderBy(query, propertyAccess, parameter, direction);
        }
        catch (Exception ex)
        {
            // Reproduces the exception that invoking the sort through reflection throws, so callers see the same type and message.
            throw new TargetInvocationException(ex);
        }
    }
#endif

    /// <summary>
    /// Observes a single entity matching a filter, backed by the shared observation pipeline.
    /// </summary>
    /// <param name="dbSet"><see cref="DbSet{TEntity}"/> to extend.</param>
    /// <param name="filter">The filter identifying the observed entity.</param>
    /// <param name="configure">Optional function to configure the query (e.g., adding includes).</param>
    /// <param name="ignoreQueryContext">Whether to ignore client paging, sorting, and total count updates.</param>
    /// <typeparam name="TEntity">Type of entity in the DbSet.</typeparam>
    /// <returns>An <see cref="ISubject{T}"/> with a single instance of the type.</returns>
    /// <remarks>
    /// The single-entity observable emits <see langword="default"/> when no entity matches — after a delete,
    /// after an update that moves the entity out of the filter, and when the initial query finds nothing —
    /// matching the MongoDB provider's <c>ObserveSingle</c> contract so the two do not diverge. The subject is
    /// always a <see cref="BehaviorSubject{T}"/>, seeded with <see langword="default"/> when the initial query
    /// finds nothing, so a subscriber attaching after the fact still replays the current "no such entity"
    /// state instead of receiving nothing until the next change.
    /// </remarks>
    static ISubject<TEntity> ObserveSingleCore<[DynamicallyAccessedMembers(EntityTypeMembers.Required)] TEntity>(
        this DbSet<TEntity> dbSet,
        Expression<Func<TEntity, bool>> filter,
        Func<DbSet<TEntity>, IQueryable<TEntity>>? configure,
        bool ignoreQueryContext)
        where TEntity : class
    {
        return dbSet.ObserveCore(
            filter,
            configure,
            ignoreQueryContext,
            entities => new BehaviorSubject<TEntity>(entities.FirstOrDefault()!),
            (entities, observable) => observable.OnNext(entities.FirstOrDefault()!));
    }

    static ISubject<TResult> ObserveCore<[DynamicallyAccessedMembers(EntityTypeMembers.Required)] TEntity, TResult>(
        this DbSet<TEntity> dbSet,
        Expression<Func<TEntity, bool>> filter,
        Func<DbSet<TEntity>, IQueryable<TEntity>>? configure,
        bool ignoreQueryContext,
        Func<IEnumerable<TEntity>, ISubject<TResult>> createSubject,
        Action<IEnumerable<TEntity>, ISubject<TResult>> onNext)
        where TEntity : class
    {
        var completedCleanup = false;
        var logger = Internals.ServiceProvider.GetRequiredService<ILogger<DbSetObserver>>();
        var changeTracker = Internals.ServiceProvider.GetRequiredService<IEntityChangeTracker>();
        var notifierFactory = Internals.ServiceProvider.GetRequiredService<IDatabaseChangeNotifierFactory>();
        var queryContextManager = Internals.ServiceProvider.GetRequiredService<IQueryContextManager>();
        var serviceScopeFactory = Internals.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        var queryContext = queryContextManager.Current;
        var observationContext = ignoreQueryContext ? queryContext with { Paging = Paging.NotPaged, Sorting = Sorting.None } : queryContext;

        var idProperty = GetIdProperty(dbSet);
        var entities = new QueryContextAwareSet<TEntity>(observationContext, idProperty);

        // Get table name and schema for database notifications
        var entityType = dbSet.EntityType;
        var tableName = entityType.GetTableName() ?? typeof(TEntity).Name;
        var schemaName = entityType.GetSchema() ?? entityType.Model.GetDefaultSchema();

        // Get column names for SQL Server SqlDependency
        // SqlDependency monitors only the columns specified in the SELECT query
        var columnNames = entityType.GetProperties()
            .Where(p => !p.IsShadowProperty())
            .Select(p => p.GetColumnName())
            .Where(name => name is not null)
            .ToList();

        // Get database information from DbContext - do this ONCE and early
        var dbContext = dbSet.GetDbContext();
        var dbContextType = dbContext.GetType();
        var databaseType = dbContext.Database.GetDatabaseType();
        var connectionString = dbContext.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Connection string is not available from the DbContext.");

        logger.StartingObservation(typeof(TEntity).Name);

        // Perform initial query synchronously to determine if we have data
        // This ensures we create the correct type of subject (Behavior vs regular)
        List<TEntity> initialEntities;
        using (var scope = serviceScopeFactory.CreateScope())
        {
            var freshDbContext = (DbContext)scope.ServiceProvider.GetRequiredService(dbContextType);
            var freshDbSet = entityType.HasSharedClrType ? freshDbContext.Set<TEntity>(entityType.Name) : freshDbContext.Set<TEntity>();
            var initialBaseQuery = ApplyConfigure(freshDbSet, configure).Where(filter);
            if (!ignoreQueryContext)
            {
                queryContext.TotalItems = initialBaseQuery.Count();
            }
            var query = BuildQuery(initialBaseQuery, observationContext, entityType);
            initialEntities = query.ToList();
        }

        entities.InitializeWithEntities(initialEntities);
        var subject = createSubject(entities);
        onNext(entities, subject);

#pragma warning disable CA2000 // Dispose objects before losing scope
        var cancellationTokenSource = new CancellationTokenSource();
        var queryExecutionSemaphore = new SemaphoreSlim(1, 1);
#pragma warning restore CA2000 // Dispose objects before losing scope
        var cancellationToken = cancellationTokenSource.Token;

        IDisposable? changeSubscription = null;
        IDatabaseChangeNotifier? databaseNotifier = null;
        var faulted = 0;

        // Common callback for both in-process and database-level changes
        void OnChangeDetected()
        {
            if (cancellationToken.IsCancellationRequested || Volatile.Read(ref faulted) == 1)
            {
                return;
            }

            // Prevent queries from running during cleanup
            if (!queryExecutionSemaphore.Wait(0))
            {
                logger.SkippingQueryDuringCleanup(typeof(TEntity).Name);
                return;
            }

            Exception? failure = null;
            try
            {
                if (cancellationToken.IsCancellationRequested || Volatile.Read(ref faulted) == 1)
                {
                    return;
                }

                logger.ChangeDetectedRequerying(typeof(TEntity).Name);

                // Create a new scope to get a fresh DbContext
                using var scope = serviceScopeFactory.CreateScope();
                var freshDbContext = (DbContext)scope.ServiceProvider.GetRequiredService(dbContextType);
                var freshDbSet = entityType.HasSharedClrType ? freshDbContext.Set<TEntity>(entityType.Name) : freshDbContext.Set<TEntity>();

                // Build the query using the fresh DbSet
                var baseQuery = ApplyConfigure(freshDbSet, configure).Where(filter);
                if (!ignoreQueryContext)
                {
                    queryContext.TotalItems = baseQuery.Count();
                }
                var newQuery = BuildQuery(baseQuery, observationContext, entityType);
                var newEntities = newQuery.ToList();
                entities.ReinitializeWithEntities(newEntities);
                onNext(entities, subject);
            }
            catch (Exception ex)
            {
                logger.UnexpectedError(typeof(TEntity).Name, ex);

                // A failure during teardown is expected and stays quiet; anything else must fault the observable.
                if (!cancellationToken.IsCancellationRequested && ex is not (OperationCanceledException or ObjectDisposedException))
                {
                    failure = ex;

                    // Terminal while still holding the semaphore, so no later change can emit after the error.
                    Volatile.Write(ref faulted, 1);
                }
            }
            finally
            {
                queryExecutionSemaphore.Release();
            }

            // Fault outside the semaphore: ending the observation runs Cleanup, which waits for it.
            if (failure is not null)
            {
                Fault(failure);
            }
        }

        void Fault(Exception exception)
        {
            try
            {
                subject.OnError(exception);
            }
            catch (Exception ex)
            {
                // A throwing observer must never reach the SaveChanges caller.
                logger.UnexpectedError(typeof(TEntity).Name, ex);
            }
            finally
            {
                try
                {
                    // Cancelling ends Watch, which runs Cleanup; later change notifications are ignored.
                    cancellationTokenSource.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // Cleanup already ran.
                }
            }
        }

        // Subscribe to in-process changes (via SaveChanges interceptor)
        var tableKey = schemaName is not null ? $"{schemaName}.{tableName}" : tableName;
        changeSubscription = changeTracker.RegisterCallback(tableKey, OnChangeDetected);

        // Subscribe to database-level changes (cross-process notifications)
        // This is done SYNCHRONOUSLY to ensure SqlDependency is ready before returning
        try
        {
            databaseNotifier = notifierFactory.Create(databaseType, connectionString);
            databaseNotifier.StartListening(tableName, schemaName, columnNames, OnChangeDetected, cancellationToken).GetAwaiter().GetResult();
            logger.DatabaseNotifierStarted(typeof(TEntity).Name);
        }
        catch (Exception ex)
        {
            // Log but don't fail - fall back to in-process only
            logger.DatabaseNotifierFailed(typeof(TEntity).Name, ex);
        }

        // Subscribe to subject completion for cleanup
        _ = subject.Subscribe(_ => { }, _ => { }, Cleanup);

        // Start background task to keep the observation alive
        _ = Task.Run(Watch);

        logger.ObservationWatchingForChanges(typeof(TEntity).Name);

        return subject;

        async Task Watch()
        {
            try
            {
                // Keep the task alive until cancelled
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                logger.ObjectDisposed();
            }
            catch (OperationCanceledException)
            {
                logger.OperationCancelled();
            }
            catch (Exception ex)
            {
                logger.UnexpectedError(typeof(TEntity).Name, ex);
            }
            finally
            {
                logger.WatchTaskEnding(typeof(TEntity).Name);
                Cleanup();
            }
        }

        void Cleanup()
        {
            if (completedCleanup)
            {
                return;
            }
            logger.CleaningUp();

            // Acquire the semaphore to ensure no queries are running
            // This will block until any in-flight query completes
            queryExecutionSemaphore.Wait();
            try
            {
                changeSubscription?.Dispose();

                if (databaseNotifier is not null)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await databaseNotifier.DisposeAsync();
                        }
                        catch
                        {
                            // Ignore cleanup errors
                        }
                    });
                }

                cancellationTokenSource?.Cancel();
                cancellationTokenSource?.Dispose();
                subject?.OnCompleted();
                logger.ObservationCompleted(typeof(TEntity).Name);
                completedCleanup = true;
            }
            finally
            {
                queryExecutionSemaphore.Release();
                queryExecutionSemaphore.Dispose();
            }
        }
    }

    static IProperty GetIdProperty<[DynamicallyAccessedMembers(EntityTypeMembers.Required)] TEntity>(DbSet<TEntity> dbSet)
        where TEntity : class
    {
        var entityType = dbSet.EntityType;
        var primaryKey = entityType.FindPrimaryKey();
        var keyProperty = primaryKey?.Properties.Count == 1 ? primaryKey.Properties[0] : null;
        var idProperty = keyProperty?.IsShadowProperty() == false
            ? keyProperty
            : GetClrIdProperty(entityType) ?? keyProperty
                ?? throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} does not have an Id property");

        if (idProperty.IsShadowProperty())
        {
            throw new InvalidOperationException($"Entity type {entityType.Name} has a shadow-property key '{idProperty.Name}' that cannot be observed");
        }

        return idProperty;
    }

    static IProperty? GetClrIdProperty(IEntityType entityType)
    {
        var property = entityType.FindProperty("Id");
        return property?.PropertyInfo?.GetMethod?.IsPublic == true ? property : null;
    }

    static IQueryable<TEntity> BuildQuery<[DynamicallyAccessedMembers(EntityTypeMembers.Required)] TEntity>(IQueryable<TEntity> query, QueryContext queryContext, IEntityType entityType)
        where TEntity : class
    {
        query = AddSorting(query, queryContext, entityType);
        query = AddPaging(query, queryContext);
        return query;
    }

    static IQueryable<TEntity> AddPaging<[DynamicallyAccessedMembers(EntityTypeMembers.Required)] TEntity>(IQueryable<TEntity> query, QueryContext queryContext)
        where TEntity : class
    {
        if (queryContext.Paging.IsPaged)
        {
            query = query
                .Skip(queryContext.Paging.Skip)
                .Take(queryContext.Paging.Size);
        }

        return query;
    }

    static IQueryable<TEntity> AddSorting<[DynamicallyAccessedMembers(EntityTypeMembers.Required)] TEntity>(IQueryable<TEntity> query, QueryContext queryContext, IEntityType entityType)
        where TEntity : class
    {
        if (queryContext.Sorting != Sorting.None)
        {
            var fieldName = queryContext.Sorting.Field.Value.ToPascalCase();
            var property = typeof(TEntity).GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public);
            var indexerProperty = property is null ? entityType.FindProperty(fieldName) : null;
            if (property is not null || indexerProperty?.IsIndexerProperty() == true)
            {
                var parameter = Expression.Parameter(typeof(TEntity), "x");
                var propertyAccess = property is not null
                    ? (Expression)Expression.Property(parameter, property)
                    : Expression.Call(typeof(EF), nameof(EF.Property), [indexerProperty!.ClrType], parameter, Expression.Constant(indexerProperty.Name));
                query = ApplyOrder(query, propertyAccess, parameter, queryContext.Sorting.Direction);
            }
        }

        return query;
    }

    static IQueryable<TEntity> ApplyOrder<TEntity>(IQueryable<TEntity> query, Expression propertyAccess, ParameterExpression parameter, SortDirection direction)
        where TEntity : class
    {
#if NET9_0_OR_GREATER
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            return OrderByWithoutDynamicCode(query, propertyAccess, parameter, direction);
        }
#endif

        return OrderByThroughReflection(query, propertyAccess, parameter, direction);
    }

    [RequiresDynamicCode("Closes Queryable.OrderBy over a property type only known at runtime.")]
    static IQueryable<TEntity> OrderByThroughReflection<TEntity>(IQueryable<TEntity> query, Expression propertyAccess, ParameterExpression parameter, SortDirection direction)
        where TEntity : class
    {
        var lambda = Expression.Lambda(propertyAccess, parameter);

        var methodName = direction == SortDirection.Ascending
            ? "OrderBy"
            : "OrderByDescending";

        var orderByMethod = typeof(Queryable)
            .GetMethods()
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(TEntity), propertyAccess.Type);

        return (IQueryable<TEntity>)orderByMethod.Invoke(null, [query, lambda])!;
    }

    static DbContext GetDbContext<[DynamicallyAccessedMembers(EntityTypeMembers.Required)] TEntity>(this DbSet<TEntity> dbSet)
        where TEntity : class
    {
        // Use IInfrastructure to get the service provider from DbSet
        var infrastructure = dbSet as IInfrastructure<IServiceProvider>;
        var serviceProvider = infrastructure?.Instance
            ?? throw new InvalidOperationException("Unable to get service provider from DbSet");

        return serviceProvider.GetRequiredService<ICurrentDbContext>().Context;
    }

    static IQueryable<TEntity> ApplyConfigure<[DynamicallyAccessedMembers(EntityTypeMembers.Required)] TEntity>(DbSet<TEntity> dbSet, Func<DbSet<TEntity>, IQueryable<TEntity>>? configure)
        where TEntity : class =>
        configure is not null ? configure(dbSet) : dbSet;

    /// <summary>
    /// Internal class used as an identifying type for logging purpose.
    /// </summary>
#pragma warning disable MA0036 // Make class static - Used as a type argument for logging
    internal sealed class DbSetObserver;
#pragma warning restore MA0036
}
