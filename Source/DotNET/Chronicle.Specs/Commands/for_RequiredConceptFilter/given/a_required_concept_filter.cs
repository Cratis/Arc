// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Concepts;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_RequiredConceptFilter.given;

public class a_required_concept_filter : Specification
{
    protected RequiredConceptFilter _filter;

    void Establish() => _filter = new RequiredConceptFilter();

    protected Task<CommandResult> Execute(object command) =>
        _filter.OnExecution(new CommandContext(CorrelationId.New(), command.GetType(), command, [], new()));

    protected record Name(string Value) : ConceptAs<string>(Value);
    protected record OrderId(Guid Value) : EventSourceId<Guid>(Value);
    protected record CustomerId(Guid Value) : EventSourceId<Guid>(Value);

    protected record CommandWithRequiredConcept(Name Name);
    protected record CommandWithNullableConcept(Name? Name);
    protected record CommandWithEventSourceKey(OrderId Id);
    protected record CommandWithKeyAttributeConcept([property: Key] Name Id);
    protected record CommandWithNonConceptProperty(string Value);

    protected record CommandWithPrimaryAndSecondaryEventSourceKeys(OrderId Id, CustomerId SecondaryId);
    protected record CommandWithEventSourceKeyAndSecondaryKeyAttribute(OrderId Id, [property: Key] Name SecondaryKey);

    protected record CommandWithComputedConcept(Name First)
    {
        public Name Derived => new(First.Value.ToUpperInvariant());
    }
}
