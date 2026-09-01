// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Arc.Testing.Commands;

/// <summary>
/// Provides a self-contained command scenario that runs commands through the real Arc command pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Instantiate this class in your test class, register any additional services via <see cref="Services"/>
/// before the first call to <see cref="Execute"/> or <see cref="Validate"/>, then call those methods to
/// drive the command through the full validation, authorization, and handler pipeline.
/// </para>
/// <para>
/// The service provider and pipeline are built lazily on the first call to <see cref="Execute"/> or
/// <see cref="Validate"/>. Register all services in the test constructor before any pipeline call.
/// </para>
/// <para>
/// No log sink is registered by default — <c>ILogger&lt;T&gt;</c> resolves as a no-op logger, so scenarios
/// stay lightweight and spawn no logging infrastructure threads. To see console output while debugging a
/// scenario, opt in before the first call to <see cref="Execute"/> or <see cref="Validate"/>:
/// <code>
/// scenario.Services.AddLogging(logging => logging.AddConsole());
/// </code>
/// </para>
/// <para>
/// Extension packages can contribute services and context values by implementing
/// <see cref="ICommandScenarioExtender"/>. Implementations are discovered automatically at construction
/// time via the type discovery system and invoked before any command is executed.
/// </para>
/// <para>
/// The scenario owns the service provider it builds and any disposable values extenders placed in
/// <see cref="Context"/>. Dispose the scenario — or let the test framework do it — to release them;
/// see <see cref="Dispose()"/> and <see cref="DisposeAsync()"/>.
/// </para>
/// <para>
/// The typical pattern with xUnit:
/// <code>
/// public class when_adding_item_to_cart
/// {
///     readonly CommandScenario&lt;AddItemToCart&gt; _scenario = new();
///
///     [Fact]
///     public async Task should_succeed()
///     {
///         var result = await _scenario.Execute(new AddItemToCart("SKU-123", 2));
///         result.ShouldBeSuccessful();
///     }
/// }
/// </code>
/// </para>
/// </remarks>
/// <typeparam name="TCommand">The type of command under test.</typeparam>
public class CommandScenario<TCommand> : IDisposable, IAsyncDisposable
{
    IServiceProvider? _serviceProvider;
    ICommandPipeline? _pipeline;
    bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandScenario{TCommand}"/> class.
    /// </summary>
    /// <remarks>
    /// Discovers all <see cref="ICommandScenarioExtender"/> implementations in loaded assemblies and
    /// invokes each one to allow them to register services and populate <see cref="Context"/>.
    /// </remarks>
    public CommandScenario()
    {
        Services = new ServiceCollection();
        Services.AddOptions();
        Services.AddLogging();
        Services.Configure<ArcOptions>(_ => { });

        Context = new Dictionary<string, object>();

        foreach (var extender in Cratis.Types.Types.Instance.FindMultiple<ICommandScenarioExtender>()
                     .Select(extenderType => Activator.CreateInstance(extenderType) as ICommandScenarioExtender
                         ?? throw new InvalidOperationException(
                             $"Failed to create an instance of command scenario extender '{extenderType.FullName}'. " +
                             "Ensure it has a public parameterless constructor.")))
        {
            extender.Extend(Services, Context);
        }
    }

    /// <summary>
    /// Gets the <see cref="IServiceCollection"/> used to configure services for the scenario.
    /// </summary>
    /// <remarks>
    /// Register additional services (mocks, stubs, fakes) here before calling
    /// <see cref="Execute"/> or <see cref="Validate"/> for the first time.
    /// </remarks>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Gets the scenario context dictionary, keyed by <see cref="string"/>.
    /// </summary>
    /// <remarks>
    /// Populated by <see cref="ICommandScenarioExtender"/> implementations during construction.
    /// Extension packages expose values from this dictionary through C# extension properties.
    /// </remarks>
    public IDictionary<string, object> Context { get; }

    /// <summary>
    /// Executes the given <typeparamref name="TCommand"/> through the real Arc command pipeline.
    /// </summary>
    /// <remarks>
    /// Builds the service provider and pipeline on the first call if they have not been built yet.
    /// </remarks>
    /// <param name="command">The command to execute.</param>
    /// <returns>A <see cref="Task{TResult}"/> that resolves to the <see cref="CommandResult"/>.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the scenario has been disposed.</exception>
    public Task<CommandResult> Execute(TCommand command)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureInitialized();
        return _pipeline!.Execute(command!, _serviceProvider!);
    }

    /// <summary>
    /// Validates the given <typeparamref name="TCommand"/> through the pipeline filters without executing the handler.
    /// </summary>
    /// <remarks>
    /// Builds the service provider and pipeline on the first call if they have not been built yet.
    /// </remarks>
    /// <param name="command">The command to validate.</param>
    /// <returns>A <see cref="Task{TResult}"/> that resolves to the <see cref="CommandResult"/>.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the scenario has been disposed.</exception>
    public Task<CommandResult> Validate(TCommand command)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureInitialized();
        return _pipeline!.Validate(command!, _serviceProvider!);
    }

    /// <summary>
    /// Disposes the scenario, releasing the service provider built for it and any disposable values in <see cref="Context"/>.
    /// </summary>
    /// <remarks>
    /// Safe to call multiple times; only the first call disposes. After disposal, calls to
    /// <see cref="Execute"/> or <see cref="Validate"/> throw <see cref="ObjectDisposedException"/>.
    /// </remarks>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Asynchronously disposes the scenario, releasing the service provider built for it and any disposable values in <see cref="Context"/>.
    /// </summary>
    /// <remarks>
    /// Prefers <see cref="IAsyncDisposable"/> on the service provider and context values when available,
    /// falling back to <see cref="IDisposable"/>. Safe to call multiple times; only the first call disposes.
    /// After disposal, calls to <see cref="Execute"/> or <see cref="Validate"/> throw <see cref="ObjectDisposedException"/>.
    /// </remarks>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the resources owned by the scenario.
    /// </summary>
    /// <param name="disposing">True when called from <see cref="Dispose()"/>, false when called from a finalizer path.</param>
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
    /// Asynchronously disposes the resources owned by the scenario.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous dispose operation.</returns>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_disposed)
        {
            return;
        }

        switch (_serviceProvider)
        {
            case IAsyncDisposable asyncDisposableProvider:
                await asyncDisposableProvider.DisposeAsync().ConfigureAwait(false);
                break;
            case IDisposable disposableProvider:
                disposableProvider.Dispose();
                break;
        }

        foreach (var value in Context.Values.Distinct())
        {
            switch (value)
            {
                case IAsyncDisposable asyncDisposableValue:
                    await asyncDisposableValue.DisposeAsync().ConfigureAwait(false);
                    break;
                case IDisposable disposableValue:
                    disposableValue.Dispose();
                    break;
            }
        }

        _serviceProvider = null;
        _pipeline = null;
        _disposed = true;
    }

    void EnsureInitialized()
    {
        if (_serviceProvider is not null)
        {
            return;
        }

        var explicitlyRegisteredServiceTypes = Services.Select(_ => _.ServiceType).ToHashSet();
        var hasExplicitDiscoverableValidatorsRegistration = explicitlyRegisteredServiceTypes.Contains(typeof(IDiscoverableValidators));

        Services.AddCratisArcCore();
        IServiceProvider? serviceProvider = null;

        // Validators are not registered here on purpose. The command pipeline constructs them on demand from the
        // command scope (see DiscoverableValidators.TryGet), so a validator taking a read model resolves the same
        // way a command handler does — no registration required.
        var discoverableValidators = new DiscoverableValidators(
            Cratis.Types.Types.Instance,
            () => serviceProvider ?? throw new InvalidOperationException("The command scenario service provider has not been built."));

        if (!hasExplicitDiscoverableValidatorsRegistration)
        {
            Services.RemoveAll<IDiscoverableValidators>();
            Services.AddSingleton<IDiscoverableValidators>(discoverableValidators);
        }

        _serviceProvider = Services.BuildServiceProvider();
        serviceProvider = _serviceProvider;
        Internals.ServiceProvider = _serviceProvider;
        _pipeline = _serviceProvider.GetRequiredService<ICommandPipeline>();
    }
}
