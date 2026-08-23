// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryEmissionGuards.when_guarding;

public class and_all_allow : given.all_dependencies
{
    ObservableQueryEmissionVerdict _result;

    void Establish() => DiscoverGuards(typeof(FirstGuard), typeof(SecondGuard));

    async Task Because() => _result = await _guards.Guard(_context);

    [Fact] void should_report_having_guards() => _guards.HasGuards.ShouldBeTrue();
    [Fact] void should_allow_the_emission() => _result.ShouldEqual(ObservableQueryEmissionVerdict.Allow);
    [Fact] void should_ask_the_first_guard() => _first.Calls.Count.ShouldEqual(1);
    [Fact] void should_ask_the_second_guard() => _second.Calls.Count.ShouldEqual(1);
    [Fact] void should_pass_the_query_name_to_the_guard() => _first.Calls[0].QueryName.Value.ShouldEqual("MyApp.Queries.GuardedQuery");
    [Fact] void should_pass_the_coerced_arguments_to_the_guard() => _first.Calls[0].Arguments["id"].ShouldEqual(42);
    [Fact] void should_pass_the_caller_identity_to_the_guard() => _first.Calls[0].Principal!.Identity!.Name.ShouldEqual("the-caller");
}
