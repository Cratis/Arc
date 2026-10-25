// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Testing.Slices;

/// <summary>
/// Provides framework-neutral given, when, and then steps over a command scenario.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <param name="command">The command to execute.</param>
public sealed class Slice<TCommand>(TCommand command) : IAsyncDisposable
    where TCommand : class
{
    SliceOutcome<TCommand>? _outcome;
    int _hasRun;

    /// <summary>
    /// Gets the command to execute.
    /// </summary>
    public TCommand Command { get; } = command;

    /// <summary>
    /// Gets the underlying scenario for additional configuration and assertions.
    /// </summary>
    public CommandScenario<TCommand> Scenario { get; } = new();

    /// <summary>
    /// Gets the outcome after the command has finished executing.
    /// </summary>
    /// <exception cref="SliceNotRun">The slice has not finished running.</exception>
    public SliceOutcome<TCommand> Then => _outcome ?? throw new SliceNotRun();

    /// <summary>
    /// Seeds prior events for the command's event source.
    /// </summary>
    /// <param name="events">The prior events in order.</param>
    /// <returns>This slice for further setup.</returns>
    public Slice<TCommand> GivenEvents(params object[] events) => GivenEvents(Command.GetEventSourceId(), events);

    /// <summary>
    /// Seeds prior events for the specified event source.
    /// </summary>
    /// <param name="eventSourceId">The event source to seed.</param>
    /// <param name="events">The prior events in order.</param>
    /// <returns>This slice for further setup.</returns>
    public Slice<TCommand> GivenEvents(EventSourceId eventSourceId, params object[] events)
    {
        Scenario.Given.ForEventSource(eventSourceId).Events(events);

        return this;
    }

    /// <summary>
    /// Pins a read model for the command's event source, subject to the scenario's decision-read rules.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <param name="readModel">The read model instance.</param>
    /// <returns>This slice for further setup.</returns>
    public Slice<TCommand> GivenReadModel<TReadModel>(TReadModel readModel)
        where TReadModel : class => GivenReadModel(Command.GetEventSourceId(), readModel);

    /// <summary>
    /// Pins a read model for the specified event source, subject to the scenario's decision-read rules.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <param name="eventSourceId">The event source to seed.</param>
    /// <param name="readModel">The read model instance.</param>
    /// <returns>This slice for further setup.</returns>
    /// <exception cref="PinnedReadModelCannotProvideDecisionToken">The command requires a protected decision read of this model.</exception>
    public Slice<TCommand> GivenReadModel<TReadModel>(EventSourceId eventSourceId, TReadModel readModel)
        where TReadModel : class
    {
        Scenario.Given.ForEventSource(eventSourceId).ReadModel(readModel);

        return this;
    }

    /// <summary>
    /// Sets the caller used by the command pipeline's authorization checks.
    /// </summary>
    /// <param name="caller">The caller to execute as.</param>
    /// <returns>This slice for further setup.</returns>
    public Slice<TCommand> GivenCaller(ClaimsPrincipal caller)
    {
        Scenario.Services.AddSingleton<ICurrentPrincipalAccessor>(new SlicePrincipal(caller));

        return this;
    }

    /// <summary>
    /// Executes the command once through the scenario's real pipeline.
    /// </summary>
    /// <returns>The command outcome with the scenario's assertion vocabulary.</returns>
    /// <exception cref="SliceAlreadyRun">Execution has already been requested.</exception>
    public async Task<SliceOutcome<TCommand>> Run()
    {
        if (Interlocked.Exchange(ref _hasRun, 1) != 0)
        {
            throw new SliceAlreadyRun();
        }

        _outcome = new(await Scenario.Execute(Command), Scenario);

        return _outcome;
    }

    /// <summary>
    /// Releases the underlying scenario and its resources.
    /// </summary>
    /// <returns>The asynchronous disposal operation.</returns>
    public ValueTask DisposeAsync() => Scenario.DisposeAsync();

    sealed class SlicePrincipal(ClaimsPrincipal caller) : ICurrentPrincipalAccessor
    {
        /// <inheritdoc/>
        public ClaimsPrincipal Current => caller;
    }
}
