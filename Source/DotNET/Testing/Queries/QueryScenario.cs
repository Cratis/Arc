// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries;
using Cratis.Arc.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Arc.Testing.Queries;

/// <summary>
/// Runs snapshot queries on a read model through the hosted Arc query pipeline without an HTTP server.
/// </summary>
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
    public async Task<QueryResult> Perform(string methodName, QueryArguments? arguments = null, Paging? paging = null, Sorting? sorting = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureInitialized();

        var name = new FullyQualifiedQueryName($"{typeof(TReadModel).FullName ?? typeof(TReadModel).Name}.{methodName}");
        var result = await _pipeline!.PerformHosted(name, arguments ?? QueryArguments.Empty, paging ?? Paging.NotPaged, sorting ?? Sorting.None, _serviceProvider!, cancellationToken);
        using (result.OwnedScope)
        {
            if (result.IsSuccess && result.Data is { } data &&
                data.GetType().GetInterfaces().Any(type => type.IsGenericType &&
                    (type.GetGenericTypeDefinition() == typeof(System.Reactive.Subjects.ISubject<>) ||
                     type.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))))
            {
                throw new StreamingQueryNotSupported(methodName);
            }

            return result;
        }
    }

    /// <summary>
    /// Releases the provider and disposable extender context values.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        (_serviceProvider as IDisposable)?.Dispose();
        foreach (var value in Context.Values.Distinct().OfType<IDisposable>())
        {
            value.Dispose();
        }

        _serviceProvider = null;
        _pipeline = null;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Asynchronously releases the provider and disposable extender context values.
    /// </summary>
    /// <returns>The disposal operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (_serviceProvider is IAsyncDisposable asyncProvider)
        {
            await asyncProvider.DisposeAsync();
        }
        else
        {
            (_serviceProvider as IDisposable)?.Dispose();
        }

        foreach (var value in Context.Values.Distinct())
        {
            if (value is IAsyncDisposable asyncValue)
            {
                await asyncValue.DisposeAsync();
            }
            else
            {
                (value as IDisposable)?.Dispose();
            }
        }

        _serviceProvider = null;
        _pipeline = null;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    void EnsureInitialized()
    {
        if (_serviceProvider is not null)
        {
            return;
        }

        var hasValidators = Services.Any(_ => _.ServiceType == typeof(IDiscoverableValidators));
        var explicitAuthorization = Services.Where(_ => _.ServiceType == typeof(ICurrentPrincipalAccessor) ||
            _.ServiceType == typeof(IAuthorizationPolicyRuntime)).ToArray();
        Services.AddCratisArcCore();
        foreach (var registration in explicitAuthorization)
        {
            Services.RemoveAll(registration.ServiceType);
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
        _pipeline = (QueryPipeline)_serviceProvider.GetRequiredService<IQueryPipeline>();
    }
}
