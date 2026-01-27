// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reactive.Subjects;
using System.Reflection;
using Cratis.Arc;
using Cratis.Arc.EntityFrameworkCore.Observe;
using Cratis.Arc.Queries;
using Cratis.Strings;
using Microsoft.EntityFrameworkCore.Infrastructure;
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
    public static ISubject<IEnumerable<TEntity>> Observe<TEntity>(
        this DbSet<TEntity> dbSet,
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? configure = null)
        where TEntity : class
    {
        filter ??= _ => true;
        return dbSet.Observe(
            () => ApplyConfigure(dbSet.Where(filter), configure),
            entities => new BehaviorSubject<IEnumerable<TEntity>>(entities),
            (entities, observable) => observable.OnNext([.. entities]));
    }

    /// <summary>
    /// Create an observable query that will observe the DbSet for changes matching the filter criteria.
    /// </summary>
    /// <param name="dbSet"><see cref="DbSet{TEntity}"/> to extend.</param>
    /// <param name="filter">Optional filter expression.</param>
    /// <param name="configure">Optional function to configure the query (e.g., adding includes).</param>
    /// <typeparam name="TEntity">Type of entity in the DbSet.</typeparam>
    /// <returns><see cref="ISubject{T}"/> with a single instance of the type.</returns>
    public static ISubject<TEntity> ObserveSingle<TEntity>(
        this DbSet<TEntity> dbSet,
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? configure = null)
        where TEntity : class
    {
        filter ??= _ => true;
        return dbSet.ObserveSingleCore(() => ApplyConfigure(dbSet.Where(filter), configure));
    }

    /// <summary>
    /// Create an observable query that will observe a single entity based on Id in the DbSet for changes.
    /// </summary>
    /// <param name="dbSet"><see cref="DbSet{TEntity}"/> to extend.</param>
    /// <param name="id">The identifier of the entity to observe.</param>
    /// <param name="configure">Optional function to configure the query (e.g., adding includes).</param>
    /// <typeparam name="TEntity">Type of entity in the DbSet.</typeparam>
    /// <typeparam name="TId">Type of id - key.</typeparam>
    /// <returns><see cref="ISubject{T}"/> with an instance of the type.</returns>
    /// <exception cref="InvalidOperationException">The exception that is thrown when the entity type does not have an Id property.</exception>
    public static ISubject<TEntity> ObserveById<TEntity, TId>(
        this DbSet<TEntity> dbSet,
        TId id,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? configure = null)
        where TEntity : class
    {
        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var idProperty = typeof(TEntity).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} does not have an Id property");

        var property = Expression.Property(parameter, idProperty);
        var constant = Expression.Constant(id, typeof(TId));
        var equals = Expression.Equal(property, constant);
        var lambda = Expression.Lambda<Func<TEntity, bool>>(equals, parameter);

        return dbSet.ObserveSingleCore(() => ApplyConfigure(dbSet.Where(lambda), configure));
    }

    static ISubject<TEntity> ObserveSingleCore<TEntity>(
        this DbSet<TEntity> dbSet,
        Func<IQueryable<TEntity>> queryBuilder)
        where TEntity : class
    {
        return dbSet.Observe<TEntity, TEntity>(
            queryBuilder,
            entities =>
            {
                var result = entities.FirstOrDefault();
                if (result is not null)
                {
                    return new BehaviorSubject<TEntity>(result);
                }

                return new Subject<TEntity>();
            },
            (entities, observable) =>
            {
                var result = entities.FirstOrDefault();
                if (result is not null)
                {
                    observable.OnNext(result);
                }
            });
    }

    static ISubject<TResult> Observe<TEntity, TResult>(
        this DbSet<TEntity> dbSet,
        Func<IQueryable<TEntity>> queryBuilder,
        Func<IEnumerable<TEntity>, ISubject<TResult>> createSubject,
        Action<IEnumerable<TEntity>, ISubject<TResult>> onNext)
        where TEntity : class
    {
        var completedCleanup = false;
        var subject = createSubject([]);
        var logger = Internals.ServiceProvider.GetRequiredService<ILogger<DbSetObserver>>();
        var changeTracker = Internals.ServiceProvider.GetRequiredService<IEntityChangeTracker>();
        var notifierFactory = Internals.ServiceProvider.GetRequiredService<IDatabaseChangeNotifierFactory>();
        var queryContextManager = Internals.ServiceProvider.GetRequiredService<IQueryContextManager>();
        var queryContext = queryContextManager.Current;

        var idProperty = GetIdProperty(dbSet);
        var entities = new QueryContextAwareSet<TEntity>(queryContext, idProperty);

        // Get table name for database notifications
        var entityType = dbSet.EntityType;
        var tableName = entityType.GetTableName() ?? typeof(TEntity).Name;

        // Get DbContext for database notifier
        var dbContext = dbSet.GetDbContext();

        logger.StartingObservation(typeof(TEntity).Name);

#pragma warning disable CA2000 // Dispose objects before losing scope
        var cancellationTokenSource = new CancellationTokenSource();
#pragma warning restore CA2000 // Dispose objects before losing scope
        var cancellationToken = cancellationTokenSource.Token;

        IDisposable? changeSubscription = null;
        IDatabaseChangeNotifier? databaseNotifier = null;

        _ = Task.Run(Watch);
        return subject;

        async Task Watch()
        {
            try
            {
                // Common callback for both in-process and database-level changes
                void OnChangeDetected()
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    try
                    {
                        logger.ChangeDetectedRequerying(typeof(TEntity).Name);
                        var baseQuery = queryBuilder();
                        queryContext.TotalItems = baseQuery.Count();
                        var newQuery = BuildQuery(baseQuery, queryContext);
                        var newEntities = newQuery.ToList();
                        entities.ReinitializeWithEntities(newEntities);
                        onNext(entities, subject);
                    }
                    catch (Exception ex)
                    {
                        logger.UnexpectedError(ex);
                    }
                }

                // Subscribe to in-process changes (via SaveChanges interceptor)
                changeSubscription = changeTracker.RegisterCallback<TEntity>(OnChangeDetected);

                // Subscribe to database-level changes (cross-process notifications)
                try
                {
                    databaseNotifier = notifierFactory.Create(dbContext);
                    await databaseNotifier.StartListeningAsync(tableName, OnChangeDetected, cancellationToken);
                    logger.DatabaseNotifierStarted(typeof(TEntity).Name);
                }
                catch (Exception ex)
                {
                    // Log but don't fail - fall back to in-process only
                    logger.DatabaseNotifierFailed(typeof(TEntity).Name, ex);
                }

                _ = subject.Subscribe(_ => { }, _ => { }, Cleanup);

                // Initial query
                var initialBaseQuery = queryBuilder();
                queryContext.TotalItems = initialBaseQuery.Count();
                var query = BuildQuery(initialBaseQuery, queryContext);
                var initialEntities = await query.ToListAsync(cancellationToken);
                entities.InitializeWithEntities(initialEntities);
                onNext(entities, subject);

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
                logger.UnexpectedError(ex);
            }
            finally
            {
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
    }

    static PropertyInfo GetIdProperty<TEntity>(DbSet<TEntity> dbSet)
        where TEntity : class
    {
        // Try to get the key from EF Core metadata first
        var entityType = dbSet.EntityType;
        var primaryKey = entityType.FindPrimaryKey();
        if (primaryKey?.Properties.Count == 1)
        {
            var keyProperty = primaryKey.Properties[0];
            var clrProperty = typeof(TEntity).GetProperty(keyProperty.Name, BindingFlags.Instance | BindingFlags.Public);
            if (clrProperty is not null)
            {
                return clrProperty;
            }
        }

        // Fall back to convention-based Id property
        return typeof(TEntity).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} does not have an Id property");
    }

    static IQueryable<TEntity> BuildQuery<TEntity>(IQueryable<TEntity> query, QueryContext queryContext)
        where TEntity : class
    {
        query = AddSorting(query, queryContext);
        query = AddPaging(query, queryContext);
        return query;
    }

    static IQueryable<TEntity> AddPaging<TEntity>(IQueryable<TEntity> query, QueryContext queryContext)
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

    static IQueryable<TEntity> AddSorting<TEntity>(IQueryable<TEntity> query, QueryContext queryContext)
        where TEntity : class
    {
        if (queryContext.Sorting != Sorting.None)
        {
            var property = typeof(TEntity).GetProperty(queryContext.Sorting.Field.ToPascalCase(), BindingFlags.Instance | BindingFlags.Public);
            if (property is not null)
            {
                var parameter = Expression.Parameter(typeof(TEntity), "x");
                var propertyAccess = Expression.Property(parameter, property);
                var lambda = Expression.Lambda(propertyAccess, parameter);

                var methodName = queryContext.Sorting.Direction == SortDirection.Ascending
                    ? "OrderBy"
                    : "OrderByDescending";

                var orderByMethod = typeof(Queryable)
                    .GetMethods()
                    .First(m => m.Name == methodName && m.GetParameters().Length == 2)
                    .MakeGenericMethod(typeof(TEntity), property.PropertyType);

                query = (IQueryable<TEntity>)orderByMethod.Invoke(null, [query, lambda])!;
            }
        }

        return query;
    }

    static DbContext GetDbContext<TEntity>(this DbSet<TEntity> dbSet)
        where TEntity : class
    {
        // Use IInfrastructure to get the service provider from DbSet
        var infrastructure = dbSet as IInfrastructure<IServiceProvider>;
        var serviceProvider = infrastructure?.Instance
            ?? throw new InvalidOperationException("Unable to get service provider from DbSet");

        return serviceProvider.GetRequiredService<ICurrentDbContext>().Context;
    }

    static IQueryable<TEntity> ApplyConfigure<TEntity>(IQueryable<TEntity> query, Func<IQueryable<TEntity>, IQueryable<TEntity>>? configure)
        where TEntity : class =>
        configure is not null ? configure(query) : query;

    /// <summary>
    /// Internal class used as an identifying type for logging purpose.
    /// </summary>
#pragma warning disable MA0036 // Make class static - Used as a type argument for logging
    internal sealed class DbSetObserver;
#pragma warning restore MA0036
}
