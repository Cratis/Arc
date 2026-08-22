// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Concepts;

namespace Cratis.Arc.Queries.for_ObservableQueryEmissionGuards.when_guarding;

public class and_the_first_guard_mutates_its_context : given.all_dependencies
{
    readonly NestedArgument _nested = new(["original"], [["original-tag"]], new ArgumentId("concept-value"));

    void Establish()
    {
        _context.Arguments["nested"] = _nested;
        DiscoverGuards(typeof(FirstGuard), typeof(SecondGuard));
        _first.OnGuard = context =>
        {
            context.Principal!.AddIdentity(new ClaimsIdentity([new Claim("mutated", "true")]));
            context.Arguments["id"] = 999;
            var nested = (NestedArgument)context.Arguments["nested"];
            nested.Values[0] = "mutated";
            nested.Values.Add("injected");
            nested.Tags[0][0] = "mutated-tag";
        };
    }

    async Task Because()
    {
        await _guards.Guard(_context);
        await _guards.Guard(_context);
    }

    [Fact] void should_give_the_second_guard_pristine_claims_on_the_first_emission() =>
        _second.Calls[0].Principal!.HasClaim("mutated", "true").ShouldBeFalse();
    [Fact] void should_give_the_second_guard_pristine_arguments_on_the_first_emission() =>
        _second.Calls[0].Arguments["id"].ShouldEqual(42);
    [Fact] void should_give_the_second_guard_pristine_nested_arguments_on_the_first_emission() =>
        ((NestedArgument)_second.Calls[0].Arguments["nested"]).Values.ShouldEqual(["original"]);
    [Fact] void should_give_the_second_guard_a_pristine_nested_array() =>
        ((NestedArgument)_second.Calls[0].Arguments["nested"]).Tags.Single().ShouldEqual(["original-tag"]);
    [Fact] void should_preserve_the_nested_concept_runtime_type() =>
        ((NestedArgument)_second.Calls[0].Arguments["nested"]).Id.ShouldEqual(new ArgumentId("concept-value"));
    [Fact] void should_give_the_second_guard_pristine_claims_on_the_later_emission() =>
        _second.Calls[1].Principal!.HasClaim("mutated", "true").ShouldBeFalse();
    [Fact] void should_give_the_second_guard_pristine_arguments_on_the_later_emission() =>
        _second.Calls[1].Arguments["id"].ShouldEqual(42);
    [Fact] void should_give_the_second_guard_pristine_nested_arguments_on_the_later_emission() =>
        ((NestedArgument)_second.Calls[1].Arguments["nested"]).Values.ShouldEqual(["original"]);
    [Fact] void should_not_mutate_the_emission_principal() => _context.Principal!.HasClaim("mutated", "true").ShouldBeFalse();
    [Fact] void should_not_mutate_the_emission_arguments() => _context.Arguments["id"].ShouldEqual(42);
    [Fact] void should_not_mutate_the_emission_nested_arguments() => _nested.Values.ShouldEqual(["original"]);
    [Fact] void should_not_mutate_the_emission_nested_array() => _nested.Tags.Single().ShouldEqual(["original-tag"]);
    [Fact] void should_give_each_guard_an_independent_principal() =>
        ReferenceEquals(_first.Calls[0].Principal, _second.Calls[0].Principal).ShouldBeFalse();
    [Fact] void should_give_each_guard_independent_arguments() =>
        ReferenceEquals(_first.Calls[0].Arguments, _second.Calls[0].Arguments).ShouldBeFalse();
    [Fact] void should_give_each_guard_independent_nested_arguments() =>
        ReferenceEquals(
            ((NestedArgument)_first.Calls[0].Arguments["nested"]).Values,
            ((NestedArgument)_second.Calls[0].Arguments["nested"]).Values).ShouldBeFalse();

    public record ArgumentId(string Value) : ConceptAs<string>(Value);

    public record NestedArgument(List<string> Values, List<string[]> Tags, ArgumentId Id);
}
