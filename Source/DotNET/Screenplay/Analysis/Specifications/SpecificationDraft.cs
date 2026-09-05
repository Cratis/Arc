// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Collects a specification while the type declaring it is being read.
/// </summary>
/// <remarks>
/// A scenario is one example, so the first step of it that cannot be read decides the outcome for all of them. The
/// reason is kept rather than the fact, because what made a scenario unreadable is the only part of it worth
/// reporting - and the first reason is kept rather than the last, since every reason after it is read from a
/// scenario already known to be incomplete.
/// </remarks>
public class SpecificationDraft
{
    readonly List<Location> _errorEvidence = [];
    readonly Dictionary<SpecificationStateModel, SpecificationStateEvidence> _stateEvidence = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<PropertyMappingModel, Location> _valueEvidence = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// Gets what had already happened when the command was issued.
    /// </summary>
    public IList<SpecificationStateModel> Given { get; } = [];

    /// <summary>
    /// Gets the events and read model states that followed.
    /// </summary>
    public IList<SpecificationStateModel> Then { get; } = [];

    /// <summary>
    /// Gets the rejections that followed, each named by the reason the source gives for it.
    /// </summary>
    public IList<string> Errors { get; } = [];

    /// <summary>
    /// Gets or sets the command that was issued.
    /// </summary>
    public SpecificationStateModel? When { get; set; }

    /// <summary>
    /// Gets why the scenario could not be read, or <see langword="null"/> while all of it still can be.
    /// </summary>
    public string? Unreadable { get; private set; }

    /// <summary>
    /// Records why a scenario cannot be read.
    /// </summary>
    /// <param name="reason">What made it unreadable.</param>
    public void CannotRead(string reason) => Unreadable ??= reason;

    /// <summary>
    /// Gets exact rejection assertion locations.
    /// </summary>
    /// <returns>The ordered rejection evidence.</returns>
    internal IReadOnlyList<Location> GetErrorEvidence() => _errorEvidence;

    /// <summary>
    /// Gets exact state-step evidence by legacy model reference.
    /// </summary>
    /// <returns>The state evidence map.</returns>
    internal IReadOnlyDictionary<SpecificationStateModel, SpecificationStateEvidence> GetStateEvidence() => _stateEvidence;

    /// <summary>
    /// Gets exact value evidence by legacy model reference.
    /// </summary>
    /// <returns>The value evidence map.</returns>
    internal IReadOnlyDictionary<PropertyMappingModel, Location> GetValueEvidence() => _valueEvidence;

    /// <summary>
    /// Adds one Given state and its exact evidence.
    /// </summary>
    /// <param name="state">The recovered legacy state.</param>
    /// <param name="artifact">The exact referenced artifact.</param>
    /// <param name="source">The exact authored step location.</param>
    internal void AddGiven(SpecificationStateModel state, ITypeSymbol artifact, Location source)
    {
        Given.Add(state);
        _stateEvidence.Add(state, new(artifact, source));
    }

    /// <summary>
    /// Adds one Then state and its exact evidence.
    /// </summary>
    /// <param name="state">The recovered legacy state.</param>
    /// <param name="artifact">The exact referenced artifact.</param>
    /// <param name="source">The exact authored step location.</param>
    internal void AddThen(SpecificationStateModel state, ITypeSymbol artifact, Location source)
    {
        Then.Add(state);
        _stateEvidence.Add(state, new(artifact, source));
    }

    /// <summary>
    /// Sets the When command and its exact evidence.
    /// </summary>
    /// <param name="state">The recovered legacy state.</param>
    /// <param name="artifact">The exact referenced command.</param>
    /// <param name="source">The exact authored invocation location.</param>
    internal void SetWhen(SpecificationStateModel state, ITypeSymbol artifact, Location source)
    {
        When = state;
        _stateEvidence.Add(state, new(artifact, source));
    }

    /// <summary>
    /// Adds exact evidence for one stated value.
    /// </summary>
    /// <param name="value">The recovered legacy value.</param>
    /// <param name="source">The exact authored value expression.</param>
    internal void AddValue(PropertyMappingModel value, Location source) => _valueEvidence.Add(value, source);

    /// <summary>
    /// Adds one rejection and its exact assertion location.
    /// </summary>
    /// <param name="error">The rejection reason, or an empty string when unnamed.</param>
    /// <param name="source">The exact authored assertion location.</param>
    internal void AddError(string error, Location source)
    {
        Errors.Add(error);
        _errorEvidence.Add(source);
    }
}
