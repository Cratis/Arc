// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using static Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_giving_state_to_a_protected_read_model.given.a_pinned_state;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver.given;

/// <summary>
/// Provides a real command scenario with separately mutable relationship endpoints.
/// </summary>
public class a_racing_chain : Specification
{
    protected CommandScenario<DecideChain> _scenario;
    protected CommandResult _result;
    readonly EventSourceId _source = EventSourceId.New();
    readonly EventSourceId _target = EventSourceId.New();

    void Establish()
    {
        _scenario = new();
        _scenario.Given.ForEventSource(_source).Events(new Linked(Guid.Parse(_target.Value)));
        _scenario.Given.ForEventSource(_target).Events(new StateChanged("original"));
    }

    protected async Task Race(bool source)
    {
        if (source) _scenario.AppendConcurrently(_source, new Linked(Guid.Parse(_target.Value)));
        else _scenario.AppendConcurrently(_target, new StateChanged("competing"));
        _result = await _scenario.Execute(new(_source), TestContext.Current.CancellationToken);
    }

    async Task Destroy() => await _scenario.DisposeAsync();

    /// <summary>
    /// Decides using a related state snapshot.
    /// </summary>
    /// <param name="Source">The source stream.</param>
    [Command, ReadProtection(ReadProtectionMode.On)]
    public record DecideChain(EventSourceId Source)
    {
        /// <summary>
        /// Produces a decision from the acquired target.
        /// </summary>
        /// <param name="target">The protected target.</param>
        /// <param name="link">The protected source relationship.</param>
        /// <returns>The decision event.</returns>
        public Decided Handle([LookUpBy(nameof(link), nameof(LinkState.Target))] State target, [LookUpBy(nameof(Source))] LinkState link) => new(target.Status);
    }

    /// <summary>
    /// Projects the source relationship.
    /// </summary>
    /// <param name="Id">The source identity.</param>
    /// <param name="Target">The target identity.</param>
    [FromEvent<Linked>]
    public record LinkState([property: Key] Guid Id, Guid Target);

    /// <summary>
    /// Establishes or changes the relationship.
    /// </summary>
    /// <param name="Target">The target identity.</param>
    [EventType]
    public record Linked(Guid Target);
}
