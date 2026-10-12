// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Globalization;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Arc.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Arc.Testing.Queries;

/// <summary>
/// Runs snapshot and observable queries on a read model through the hosted Arc query pipeline without an HTTP server.
/// </summary>
/// <remarks>
/// Register services before the first call to <see cref="Perform(string, QueryArguments?, Paging?, Sorting?, CancellationToken)"/>.
/// The scenario owns its provider and extender context values; use <see cref="Dispose()"/> or <see cref="DisposeAsync()"/>
/// to release them. A snapshot returned from an identity-bound scope is materialized before that scope is released.
/// </remarks>
/// <typeparam name="TReadModel">The read model containing the static query methods.</typeparam>
public class QueryScenario<TReadModel> : IDisposable, IAsyncDisposable
{
    readonly List<QueryObservation> _observations = [];
    IServiceProvider? _serviceProvider;
    QueryPipeline? _pipeline;
    bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryScenario{TReadModel}"/> class.
    /// </summary>
    public QueryScenario()
    {
        Services = new ServiceCollection();
        Services.AddOptions();
        Services.AddLogging();
        Services.Configure<ArcOptions>(_ => { });
        Context = new Dictionary<string, object>();

        foreach (var extender in TypesServiceCollectionExtensions.CurrentTypeUniverse().FindMultiple<IQueryScenarioExtender>()
                     .Select(type => Activator.CreateInstance(type) as IQueryScenarioExtender
                         ?? throw new InvalidOperationException($"Failed to create an instance of query scenario extender '{type.FullName}'. Ensure it has a public parameterless constructor.")))
        {
            extender.Extend(Services, Context);
        }
    }

    /// <summary>
    /// Gets the service registrations; configure these before calling <see cref="Perform(string, QueryArguments?, Paging?, Sorting?, CancellationToken)"/>.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Gets context values supplied by scenario extenders.
    /// </summary>
    public IDictionary<string, object> Context { get; }

    /// <summary>
    /// Runs a static query method on <typeparamref name="TReadModel"/> through argument conversion, filters, and hosted authorization.
    /// </summary>
    /// <param name="methodName">The query method name, not a fully qualified query name.</param>
    /// <param name="arguments">The query's named arguments.</param>
    /// <param name="paging">Optional paging.</param>
    /// <param name="sorting">Optional sorting.</param>
    /// <param name="cancellationToken">Execution cancellation.</param>
    /// <returns>The query result after its owned execution scope has been released.</returns>
    /// <exception cref="ObjectDisposedException">The scenario has been disposed.</exception>
    /// <exception cref="StreamingQueryNotSupported">The query returns an asynchronous stream; use Observe instead.</exception>
    /// <exception cref="QueryScenarioRequiresArcQueryPipeline">The configured query pipeline is not Arc's hosted pipeline.</exception>
    public async Task<QueryResult> Perform(string methodName, QueryArguments? arguments = null, Paging? paging = null, Sorting? sorting = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureInitialized();

        var method = ModelBoundQueryMethod.GetMethods(typeof(TReadModel), candidate => candidate.IsValidQueryFor(typeof(TReadModel)))
            .FirstOrDefault(candidate => candidate.Name == methodName);
        if (method is not null && IsAsyncEnumerable(UnwrapTask(method.ReturnType)))
        {
            throw new StreamingQueryNotSupported(methodName);
        }

        var name = new FullyQualifiedQueryName($"{typeof(TReadModel).FullName ?? typeof(TReadModel).Name}.{methodName}");
        var result = await _pipeline!.PerformHosted(name, arguments ?? QueryArguments.Empty, paging ?? Paging.NotPaged, sorting ?? Sorting.None, _serviceProvider!, cancellationToken);
        if (result.IsSuccess && result.Data is IObservable<TReadModel> or IObservable<IEnumerable<TReadModel>>)
        {
            await using var observation = await CreateObservation(result, ObservableQueryHttp.DefaultWaitForFirstResultTimeout, true, cancellationToken);
            return observation.Initial;
        }

        using (result.OwnedScope)
        {
            if (result.IsSuccess && result.Data is { } data && IsAsyncEnumerable(data.GetType()))
            {
                switch (data)
                {
                    case IAsyncDisposable asyncDisposable:
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                        break;
                    case IDisposable disposable:
                        disposable.Dispose();
                        break;
                }

                throw new StreamingQueryNotSupported(methodName);
            }

            if (result.IsSuccess && result.OwnedScope is not null && result.Data is IEnumerable sequence and not ICollection)
            {
                result.Data = sequence.Cast<TReadModel>().ToList();
            }

            return result;
        }
    }

    /// <summary>
    /// Observes a query's first result and subsequent emissions through the hosted pipeline.
    /// </summary>
    /// <param name="methodName">The query method name.</param>
    /// <param name="arguments">The query's named arguments.</param>
    /// <param name="paging">Optional paging.</param>
    /// <param name="sorting">Optional sorting.</param>
    /// <param name="timeout">The first-emission deadline; defaults to five seconds.</param>
    /// <param name="cancellationToken">Cancellation for waiting for the first emission, not the observation's lifetime.</param>
    /// <returns>An observation owned by this scenario.</returns>
    /// <exception cref="ObjectDisposedException">The scenario has been disposed.</exception>
    /// <exception cref="TimeoutException">No first emission arrives before the deadline.</exception>
    public async Task<QueryObservation> Observe(
        string methodName,
        QueryArguments? arguments = null,
        Paging? paging = null,
        Sorting? sorting = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureInitialized();
        var name = new FullyQualifiedQueryName($"{typeof(TReadModel).FullName ?? typeof(TReadModel).Name}.{methodName}");
        var result = await _pipeline!.PerformHosted(name, arguments ?? QueryArguments.Empty, paging ?? Paging.NotPaged, sorting ?? Sorting.None, _serviceProvider!, CancellationToken.None);

        return await CreateObservation(result, timeout ?? TimeSpan.FromSeconds(5), false, cancellationToken);
    }

    /// <summary>
    /// Releases observations, the provider and disposable extender context values.
    /// </summary>
    /// <remarks>Safe to call multiple times; subsequent calls to <see cref="Perform(string, QueryArguments?, Paging?, Sorting?, CancellationToken)"/> throw.</remarks>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Asynchronously releases the provider and disposable extender context values.
    /// </summary>
    /// <returns>The disposal operation.</returns>
    /// <remarks>Prefers asynchronous disposal when available. Safe to call multiple times.</remarks>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the resources owned by the scenario.
    /// </summary>
    /// <param name="disposing">Whether to release managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            foreach (var observation in _observations)
            {
                observation.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            (_serviceProvider as IDisposable)?.Dispose();
            foreach (var value in Context.Values.Distinct().OfType<IDisposable>())
            {
                value.Dispose();
            }
        }

        _serviceProvider = null;
        _pipeline = null;
        _disposed = true;
    }

    /// <summary>
    /// Asynchronously releases the resources owned by the scenario.
    /// </summary>
    /// <returns>The disposal operation.</returns>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var observation in _observations)
        {
            await observation.DisposeAsync().ConfigureAwait(false);
        }

        if (_serviceProvider is IAsyncDisposable asyncProvider)
        {
            await asyncProvider.DisposeAsync().ConfigureAwait(false);
        }
        else
        {
            (_serviceProvider as IDisposable)?.Dispose();
        }

        foreach (var value in Context.Values.Distinct())
        {
            if (value is IAsyncDisposable asyncValue)
            {
                await asyncValue.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                (value as IDisposable)?.Dispose();
            }
        }

        _serviceProvider = null;
        _pipeline = null;
        _disposed = true;
    }

    static Type UnwrapTask(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>)
        ? type.GetGenericArguments()[0]
        : type;

    static bool IsAsyncEnumerable(Type type) =>
        type.GetInterfaces().Append(type).Any(candidate => candidate.IsGenericType &&
            candidate.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>));

    async Task<QueryObservation> CreateObservation(QueryResult result, TimeSpan timeout, bool timeoutAsResult, CancellationToken cancellationToken)
    {
        if (!result.IsSuccess)
        {
            result.OwnedScope?.Dispose();
            return RegisterObservation(new(result), !timeoutAsResult);
        }

        if (result.Data is not IObservable<TReadModel> and not IObservable<IEnumerable<TReadModel>> and not IAsyncEnumerable<TReadModel>)
        {
            using (result.OwnedScope)
            {
                if (result.OwnedScope is not null && result.Data is IEnumerable sequence and not ICollection)
                {
                    result.Data = sequence.Cast<TReadModel>().ToList();
                }
            }

            return RegisterObservation(new(result), !timeoutAsResult);
        }

        var emissions = new ObservedQueryEmissions(result, data => data is IEnumerable sequence and not string
            ? sequence.Cast<TReadModel>().ToList()
            : data);
        var observation = RegisterObservation(new(result, emissions), !timeoutAsResult);
        try
        {
            emissions.Start<TReadModel>();
            await observation.ReadInitial(timeout, cancellationToken);
            return observation;
        }
        catch (TimeoutException) when (timeoutAsResult)
        {
            await observation.DisposeAsync();
            return new(QueryResult.Error(
                result.CorrelationId,
                $"Timed out waiting {timeout.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture)} seconds for the first observable query result."));
        }
        catch
        {
            await observation.DisposeAsync();
            throw;
        }
    }

    QueryObservation RegisterObservation(QueryObservation observation, bool track)
    {
        if (track)
        {
            _observations.Add(observation);
        }

        return observation;
    }

    void EnsureInitialized()
    {
        if (_serviceProvider is not null)
        {
            return;
        }

        var hasValidators = Services.Any(_ => _.ServiceType == typeof(IDiscoverableValidators));
        Services.AddCratisArcCoreKeepingAuthorization();
        Services.RemoveAll<IQueryPerformerProviders>();
        Services.AddSingleton<IQueryPerformerProviders, ScenarioQueryPerformerProviders<TReadModel>>();
        IServiceProvider? serviceProvider = null;
        if (!hasValidators)
        {
            Services.RemoveAll<IDiscoverableValidators>();
            Services.AddSingleton<IDiscoverableValidators>(new DiscoverableValidators(
                TypesServiceCollectionExtensions.CurrentTypeUniverse(),
                () => serviceProvider ?? throw new InvalidOperationException("The query scenario service provider has not been built.")));
        }

        _serviceProvider = Services.BuildServiceProvider();
        serviceProvider = _serviceProvider;

        // No host is started here, so the reporter AddCratisArcCore registers never runs; report skips directly.
        GeneratedMetadataRegistration.LogSkippedProjectAssemblies(_serviceProvider);
        _pipeline = _serviceProvider.GetRequiredService<IQueryPipeline>() as QueryPipeline
            ?? throw new QueryScenarioRequiresArcQueryPipeline();
    }
}
