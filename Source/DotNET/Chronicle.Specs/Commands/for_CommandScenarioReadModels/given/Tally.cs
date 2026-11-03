// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenarioReadModels.given;

/// <summary>
/// A read model materialized by a reducer.
/// </summary>
/// <param name="Total">The sum of every raised amount.</param>
public record Tally(int Total);

/// <summary>
/// The reducer that materializes <see cref="Tally"/>.
/// </summary>
public class TallyReducer : IReducerFor<Tally>
{
    public Tally Raised(TallyRaised @event, Tally? current) => new((current?.Total ?? 0) + @event.Amount);
}

/// <summary>
/// A read model materialized by a model-bound projection.
/// </summary>
/// <param name="Id">The event source the note belongs to.</param>
/// <param name="Text">The latest note text.</param>
[FromEvent<NoteWritten>]
public record Note([property: Key] Guid Id, string Text);

/// <summary>
/// A read model the protected decision route folds as its decision.
/// </summary>
/// <param name="Id">The event source the guard belongs to.</param>
/// <param name="Status">The guard status.</param>
[FromEvent<GuardChanged>]
public record Guard([property: Key] Guid Id, string Status);

/// <summary>
/// Reports the materialized tally and note through the legacy route.
/// </summary>
/// <param name="EventSourceId">The event source to report for.</param>
[Command]
public record ReportTally(EventSourceId EventSourceId)
{
    public Reported Handle(Tally? tally, Note? note) => new(note?.Text ?? Reported.Absent, tally?.Total ?? Reported.NoTotal);
}

/// <summary>
/// Reports the materialized tally and note through the protected decision route.
/// </summary>
/// <param name="EventSourceId">The event source to report for.</param>
[Command]
[ProtectedDecision]
public record ReportUnderDecision(EventSourceId EventSourceId)
{
    public Reported Handle(DecisionRead<Guard> guard, Tally? tally, Note? note) => new(note?.Text ?? Reported.Absent, tally?.Total ?? Reported.NoTotal);
}

/// <summary>
/// Appends a note that builds on the note read before the append, through the protected decision route.
/// </summary>
/// <param name="EventSourceId">The event source to revise the note for.</param>
/// <param name="Text">The text to add.</param>
[Command]
[ProtectedDecision]
public record ReviseNote(EventSourceId EventSourceId, string Text)
{
    public async Task<NoteWritten> Handle(DecisionRead<Guard> guard, IReadModels readModels)
    {
        var note = await readModels.GetInstanceById<Note>(EventSourceId.Value);
        return new($"{note?.Text ?? Reported.Absent}>{Text}");
    }
}

[EventType("0b6f4f8e-5d0c-4a52-9c3b-4f1f6c1d2a01")]
public record TallyRaised(int Amount);

[EventType("0b6f4f8e-5d0c-4a52-9c3b-4f1f6c1d2a02")]
public record NoteWritten(string Text);

[EventType("0b6f4f8e-5d0c-4a52-9c3b-4f1f6c1d2a03")]
public record GuardChanged(string Status);

[EventType("0b6f4f8e-5d0c-4a52-9c3b-4f1f6c1d2a04")]
public record Reported(string Note, int Total)
{
    /// <summary>
    /// The note reported when no note exists.
    /// </summary>
    public const string Absent = "absent";

    /// <summary>
    /// The total reported when no tally exists.
    /// </summary>
    public const int NoTotal = -1;
}
