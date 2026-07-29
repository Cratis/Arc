// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

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
    /// Records why the scenario cannot be read.
    /// </summary>
    /// <param name="reason">What made it unreadable.</param>
    public void CannotRead(string reason) => Unreadable ??= reason;
}
