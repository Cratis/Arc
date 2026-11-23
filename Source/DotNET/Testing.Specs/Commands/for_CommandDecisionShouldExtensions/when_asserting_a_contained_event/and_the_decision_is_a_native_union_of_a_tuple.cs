// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using Cratis.Arc.Validation;

namespace Cratis.Arc.Testing.Commands.for_CommandDecisionShouldExtensions.when_asserting_a_contained_event;

public class and_the_decision_is_a_native_union_of_a_tuple : Specification
{
    given.ItemAdded _event = new(3);
    given.ItemAdded _matched;

    void Because()
    {
        Outcome result = (Guid.Empty, _event);
        _matched = result.ShouldContainEvent<given.ItemAdded>();
    }

    [Fact] void should_expand_the_active_native_tuple_case() => _matched.ShouldEqual(_event);

    // Compiler-generated union struct; equality is not meaningful for a handler return.
#pragma warning disable CA1815
    public readonly union Outcome((Guid, given.ItemAdded), ValidationResult);
#pragma warning restore CA1815
}
#endif
