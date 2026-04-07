// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Specifications;
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
/// The service provider is built by the internal <c>Establish</c> method which runs before
/// any derived <c>Establish</c> or <c>Because</c> methods, so all services registered in subclass
/// constructors are available when the command executes.
/// </para>
/// </remarks>
/// <typeparam name="TCommand">The type of command under test.</typeparam>
public abstract class CommandScenarioFor<TCommand> : Specification
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
    /// Subclass constructors may register additional services here before <see cref="Establish"/>
    /// builds the service provider.
    /// </remarks>
    protected IServiceCollection Services { get; }

    /// <summary>
    /// Gets the <see cref="ICommandPipeline"/> used to execute commands.
    /// </summary>
    /// <remarks>Available after <see cref="Establish"/> has run.</remarks>
    protected ICommandPipeline Pipeline => _pipeline!;

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

    /// <summary>
    /// Builds the service provider and resolves the <see cref="ICommandPipeline"/>.
    /// </summary>
    /// <remarks>
    /// Called automatically by the <see cref="Specification"/> framework before any derived
    /// <c>Establish</c> methods and before <c>Because</c>, ensuring the pipeline is ready.
    /// </remarks>
    void Establish()
    {
        Services.AddCratisArcCore();
        _serviceProvider = Services.BuildServiceProvider();
        Internals.ServiceProvider = _serviceProvider;
        _pipeline = _serviceProvider.GetRequiredService<ICommandPipeline>();
    }
}
