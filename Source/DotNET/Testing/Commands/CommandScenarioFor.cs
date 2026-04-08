// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Testing.Commands;

/// <summary>
/// Base class for command scenario specifications that run commands through the real Arc command pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Inheriting from this class sets up the complete Arc service infrastructure (type discovery,
/// validation filters, authorization filters, command handlers) using the real implementations.
/// </para>
/// <para>
/// Subclasses can register additional services during construction by accessing <see cref="Services"/>.
/// The service provider is built during <see cref="InitializeAsync"/>, so all services registered
/// in subclass constructors are available when the command executes.
/// </para>
/// <para>
/// Override <see cref="InitializeAsync"/> to perform setup (seed state, configure mocks) and execute
/// the command under test. Always call <c>await base.InitializeAsync()</c> first so that the pipeline
/// is ready before calling <see cref="Execute"/> or <see cref="Validate"/>.
/// </para>
/// <para>
/// To integrate with xUnit's lifecycle, have your test class also implement <c>IAsyncLifetime</c>:
/// <code>
/// public class when_registering_author
///     : CommandScenarioFor&lt;RegisterAuthor&gt;, IAsyncLifetime
/// {
///     public override async Task InitializeAsync()
///     {
///         await base.InitializeAsync();
///         Result = await Execute(new RegisterAuthor("Jane Austen"));
///     }
/// }
/// </code>
/// xUnit discovers the <c>IAsyncLifetime</c> interface and calls <c>InitializeAsync</c> automatically.
/// </para>
/// </remarks>
/// <typeparam name="TCommand">The type of command under test.</typeparam>
public abstract class CommandScenarioFor<TCommand>
{
    IServiceProvider? _serviceProvider;
    ICommandPipeline? _pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandScenarioFor{TCommand}"/> class.
    /// </summary>
    protected CommandScenarioFor()
    {
        Services = new ServiceCollection();
        Services.AddOptions();
        Services.AddLogging(logging => logging.AddConsole());
        Services.Configure<ArcOptions>(_ => { });
    }

    /// <summary>
    /// Gets the <see cref="IServiceCollection"/> used to configure services for the scenario.
    /// </summary>
    /// <remarks>
    /// Register additional services (mocks, stubs, fakes) here before <see cref="InitializeAsync"/>
    /// builds the service provider.
    /// </remarks>
    protected IServiceCollection Services { get; }

    /// <summary>
    /// Gets the <see cref="ICommandPipeline"/> used to execute commands.
    /// </summary>
    /// <remarks>Available after <see cref="InitializeAsync"/> has run.</remarks>
    protected ICommandPipeline Pipeline => _pipeline!;

    /// <summary>
    /// Builds the Arc service provider and command pipeline.
    /// </summary>
    /// <remarks>
    /// Override this method to perform scenario-specific setup (seeding state, running the command).
    /// Always call <c>await base.InitializeAsync()</c> first to ensure the pipeline is ready.
    /// </remarks>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public virtual Task InitializeAsync()
    {
        Services.AddCratisArcCore();
        _serviceProvider = Services.BuildServiceProvider();
        Internals.ServiceProvider = _serviceProvider;
        _pipeline = _serviceProvider.GetRequiredService<ICommandPipeline>();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs cleanup after the test scenario completes.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public virtual Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Executes the given <typeparamref name="TCommand"/> through the real Arc command pipeline.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <returns>A <see cref="Task{TResult}"/> that resolves to the <see cref="CommandResult"/>.</returns>
    protected Task<CommandResult> Execute(TCommand command) =>
        _pipeline!.Execute(command!, _serviceProvider!);

    /// <summary>
    /// Validates the given <typeparamref name="TCommand"/> through the pipeline filters without executing the handler.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <returns>A <see cref="Task{TResult}"/> that resolves to the <see cref="CommandResult"/>.</returns>
    protected Task<CommandResult> Validate(TCommand command) =>
        _pipeline!.Validate(command!, _serviceProvider!);
}
