// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Queries.for_ObservableQueryEmissionGuards.when_guarding;

public class and_the_first_guard_mutates_its_context : given.all_dependencies
{
    void Establish()
    {
        DiscoverGuards(typeof(FirstGuard), typeof(SecondGuard));
        _first.OnGuard = context =>
        {
            context.Principal!.AddIdentity(new ClaimsIdentity([new Claim("mutated", "true")]));
            context.Arguments.Clear();
            context.Arguments["id"] = 999;
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
    [Fact] void should_give_the_second_guard_pristine_claims_on_the_later_emission() =>
        _second.Calls[1].Principal!.HasClaim("mutated", "true").ShouldBeFalse();
    [Fact] void should_give_the_second_guard_pristine_arguments_on_the_later_emission() =>
        _second.Calls[1].Arguments["id"].ShouldEqual(42);
    [Fact] void should_not_mutate_the_emission_principal() => _context.Principal!.HasClaim("mutated", "true").ShouldBeFalse();
    [Fact] void should_not_mutate_the_emission_arguments() => _context.Arguments["id"].ShouldEqual(42);
    [Fact] void should_give_each_guard_an_independent_principal() =>
        ReferenceEquals(_first.Calls[0].Principal, _second.Calls[0].Principal).ShouldBeFalse();
    [Fact] void should_give_each_guard_independent_arguments() =>
        ReferenceEquals(_first.Calls[0].Arguments, _second.Calls[0].Arguments).ShouldBeFalse();
}
