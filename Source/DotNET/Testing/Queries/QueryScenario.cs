// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reactive.Subjects;
using System.Reflection;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Arc.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Arc.Testing.Queries;

/// <summary>
/// Runs snapshot queries on a read model through the hosted Arc query pipeline without an HTTP server.
/// </summary>
/// <remarks>
/// Register services before the first call to <see cref="Perform(string, QueryArguments?, Paging?, Sorting?, CancellationToken)"/>.
/// The scenario owns its provider and extender context values; use <see cref="Dispose()"/> or <see cref="DisposeAsync()"/>
/// to release them. A snapshot returned from an identity-bound scope is materialized before that scope is released.
/// </remarks>
/// <typeparam name="TReadModel">The read model containing the static query methods.</typeparam>
public class QueryScenario<TReadModel> : IDisposable, IAsyncDisposable
{
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
    /// Runs a static snapshot method on <typeparamref name="TReadModel"/> through argument conversion, filters, and hosted authorization.
    /// </summary>
    /// <param name="methodName">The query method name, not a fully qualified query name.</param>
    /// <param name="arguments">The query's named arguments.</param>
    /// <param name="paging">Optional paging.</param>
    /// <param name="sorting">Optional sorting.</param>
    /// <param name="cancellationToken">Execution cancellation.</param>
    /// <returns>The query result after its owned execution scope has been released.</returns>
    /// <exception cref="ObjectDisposedException">The scenario has been disposed.</exception>
    /// <exception cref="StreamingQueryNotSupported">The query returns an observable or asynchronous stream.</exception>
    /// <exception cref="QueryScenarioRequiresArcQueryPipeline">The configured query pipeline is not Arc's hosted pipeline.</exception>
    public async Task<QueryResult> Perform(string methodName, QueryArguments? arguments = null, Paging? paging = null, Sorting? sorting = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureInitialized();

        var method = typeof(TReadModel).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .FirstOrDefault(candidate => candidate.Name == methodName && ModelBoundQueryMethod.IsCandidate(candidate) && candidate.IsValidQueryFor(typeof(TReadModel)));
        if (method is not null && IsStreamingType(UnwrapTask(method.ReturnType)))
        {
            throw new StreamingQueryNotSupported(methodName);
        }

        var name = new FullyQualifiedQueryName($"{typeof(TReadModel).FullName ?? typeof(TReadModel).Name}.{methodName}");
        var result = await _pipeline!.PerformHosted(name, arguments ?? QueryArguments.Empty, paging ?? Paging.NotPaged, sorting ?? Sorting.None, _serviceProvider!, cancellationToken);
        using (result.OwnedScope)
        {
            if (result.IsSuccess && result.Data is { } data && IsStreamingType(data.GetType()))
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
    /// Releases the provider and disposable extender context values.
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

    static bool IsStreamingType(Type type) =>
        type.GetInterfaces().Append(type).Any(candidate => candidate.IsGenericType &&
            (candidate.GetGenericTypeDefinition() == typeof(ISubject<>) ||
             candidate.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>)));

    void EnsureInitialized()
    {
        if (_serviceProvider is not null)
        {
            return;
        }

        var hasValidators = Services.Any(_ => _.ServiceType == typeof(IDiscoverableValidators));
        var explicitAuthorization = Services.Where(_ => _.ServiceType == typeof(ICurrentPrincipalAccessor) ||
            _.ServiceType == typeof(ICurrentPrincipalOverride) ||
            _.ServiceType == typeof(IAuthorizationPolicyRuntime)).ToArray();
        Services.AddCratisArcCore();
        foreach (var type in explicitAuthorization.Select(_ => _.ServiceType).Distinct())
        {
            Services.RemoveAll(type);
        }
        foreach (var registration in explicitAuthorization)
        {
            Services.Add(registration);
        }
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
        _pipeline = _serviceProvider.GetRequiredService<IQueryPipeline>() as QueryPipeline
            ?? throw new QueryScenarioRequiresArcQueryPipeline();
    }
}
