// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Arc.Chronicle.Commands.for_CommandScenario;
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.Commands.for_DecisionReadAdmissions;

[FromEvent<AdmissionFlagRaised>]
public record AdmittedFlag([property: Key] Guid Id);

[FromEvent<AdmissionShelfInstalled>]
public record AdmissionShelf(
    [property: Key] Guid Id,
    [ChildrenFrom<AdmissionBookShelved>(key: nameof(AdmissionBookShelved.BookId), parentKey: nameof(AdmissionBookShelved.ShelfId))]
    IEnumerable<AdmissionShelvedBook> Books);

public record AdmissionShelvedBook(Guid Id);

public record AdmissionProvided;

[Command]
[ProtectedDecision]
public record DecideOnAdmittedShapes(EventSourceId EventSourceId)
{
    public AdmissionDecided Handle(DecisionRead<AdmittedFlag> flag) => new();
}

[Command]
[ProtectedDecision]
public record DecideOnRefusedShapes(EventSourceId EventSourceId)
{
    public AdmissionProvided Provide(DecisionRead<LedgerBalance> balance) => new();

    public AdmissionDecided Handle(DecisionRead<AdmittedFlag> flag, DecisionRead<AdmissionShelf> shelf, AdmissionProvided provided) => new();
}

[ProtectedDecision]
public abstract record InheritedAdmissionDecision(EventSourceId EventSourceId)
{
    public AdmissionDecided Handle(DecisionRead<AdmissionShelf> shelf) => new();
}

[Command]
[ProtectedDecision]
public record DecideThroughInheritedHandle(EventSourceId EventSourceId) : InheritedAdmissionDecision(EventSourceId);

[Command]
[ProtectedDecision]
public record DecideOnEnumeratedRefusedShapes(EventSourceId EventSourceId)
{
    public AdmissionDecided Handle(IEnumerable<DecisionRead<AdmissionShelf>> shelves) => new();
}

[Command]
[ProtectedDecision]
public record DecideWithRefusedShapeInHelper(EventSourceId EventSourceId)
{
    public AdmissionDecided Handle(DecisionRead<AdmittedFlag> flag) => new();

    public static AdmissionDecided Describe(DecisionRead<AdmissionShelf> shelf) => new();

    internal AdmissionDecided Explain(DecisionRead<AdmissionShelf> shelf) => new();
}

[Command]
[Unprotected]
public record ReadRefusedShapeUnprotected(EventSourceId EventSourceId)
{
    public AdmissionDecided Handle(DecisionRead<AdmissionShelf> shelf) => new();
}

[EventType("25d9f1b4-3ce7-4329-b878-fe08265ae119")]
public record AdmissionFlagRaised;

[EventType("68e3b3a8-de0a-4204-9286-83a143feb7f2")]
public record AdmissionShelfInstalled;

[EventType("ab2fea93-c8fd-4cb1-8629-680543bd4c48")]
public record AdmissionBookShelved(Guid ShelfId, Guid BookId);

[EventType("0b8f5f7e-2c1e-4b8e-9a51-6f3d2a7c9e14")]
public record AdmissionDecided;
