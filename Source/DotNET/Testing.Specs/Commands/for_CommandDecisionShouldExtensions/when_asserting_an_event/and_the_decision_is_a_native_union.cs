// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using Cratis.Arc.Validation;
using Cratis.Monads;

namespace Cratis.Arc.Testing.Commands.for_CommandDecisionShouldExtensions.when_asserting_an_event;

public class and_the_decision_is_a_native_union : Specification
{
    given.ItemAdded _event = new(3);
    given.ItemAdded _matched;

    void Because()
    {
        Result<Outcome, string> result = new Outcome(_event);
        _matched = result.ShouldBeEvent<given.ItemAdded>();
    }

    [Fact] void should_unwrap_the_active_native_case_inside_result() => _matched.ShouldEqual(_event);

    // Compiler-generated union struct; equality is not meaningful for a handler return.
#pragma warning disable CA1815
    public readonly union Outcome(given.ItemAdded, ValidationResult);
#pragma warning restore CA1815
}
#endif
