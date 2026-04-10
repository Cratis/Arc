// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
/// Extension packages can contribute services and context values by implementing
/// <see cref="ICommandScenarioExtender"/>. Implementations are discovered automatically at construction
/// time via the type discovery system and invoked before any command is executed.
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
public class CommandScenario<TCommand>
{
    IServiceProvider? _serviceProvider;
    ICommandPipeline? _pipeline;

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
        Services.AddLogging(logging => logging.AddConsole());
        Services.Configure<ArcOptions>(_ => { });

        Context = new Dictionary<Type, object>();

        foreach (var extenderType in Cratis.Types.Types.Instance.FindMultiple<ICommandScenarioExtender>())
        {
            var extender = (ICommandScenarioExtender)Activator.CreateInstance(extenderType)!;
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
    /// Gets the scenario context dictionary, keyed by <see cref="Type"/>.
    /// </summary>
    /// <remarks>
    /// Populated by <see cref="ICommandScenarioExtender"/> implementations during construction.
    /// Extension packages expose values from this dictionary through C# extension properties.
    /// </remarks>
    public IDictionary<Type, object> Context { get; }

    /// <summary>
    /// Executes the given <typeparamref name="TCommand"/> through the real Arc command pipeline.
    /// </summary>
    /// <remarks>
    /// Builds the service provider and pipeline on the first call if they have not been built yet.
    /// </remarks>
    /// <param name="command">The command to execute.</param>
    /// <returns>A <see cref="Task{TResult}"/> that resolves to the <see cref="CommandResult"/>.</returns>
    public Task<CommandResult> Execute(TCommand command)
    {
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
    public Task<CommandResult> Validate(TCommand command)
    {
        EnsureInitialized();
        return _pipeline!.Validate(command!, _serviceProvider!);
    }

    void EnsureInitialized()
    {
        if (_serviceProvider is not null)
        {
            return;
        }

        Services.AddCratisArcCore();
        _serviceProvider = Services.BuildServiceProvider();
        Internals.ServiceProvider = _serviceProvider;
        _pipeline = _serviceProvider.GetRequiredService<ICommandPipeline>();
    }
}
