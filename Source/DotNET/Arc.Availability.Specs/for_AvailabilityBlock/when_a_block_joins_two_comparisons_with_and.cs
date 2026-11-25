// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityBlock;

public class when_a_block_joins_two_comparisons_with_and : Specification
{
    AvailabilityDefinition _definition;
    AvailabilityResult _passing;
    AvailabilityResult _failing;

    async Task Because()
    {
        _definition = new given.TypedDeclarations.ConditionsOfOneWhen().Define(_ => throw new InvalidOperationException());
        var evaluator = new AvailabilityEvaluator(new AvailabilityCatalog());
        _passing = await evaluator.Evaluate(_definition.Expression, Requests.For(null, ambient: Vessel(5, given.TypedDeclarations.Package.Standard)));
        _failing = await evaluator.Evaluate(_definition.Expression, Requests.For(null, ambient: Vessel(5, given.TypedDeclarations.Package.Basic)));
    }

    static Dictionary<string, object?> Vessel(int score, given.TypedDeclarations.Package package) =>
        new() { ["vessel"] = new given.TypedDeclarations.Vessel(true, score, package) };

    [Fact] void should_be_one_when_over_the_provided_read_model() => _definition.Expression.ShouldBeOfExactType<AvailableWhen>();
    [Fact] void should_hold_when_both_comparisons_hold() => _passing.IsAvailable.ShouldBeTrue();
    [Fact] void should_not_hold_when_one_comparison_fails() => _failing.Outcome.ShouldEqual(AvailabilityOutcome.Unavailable);
}
