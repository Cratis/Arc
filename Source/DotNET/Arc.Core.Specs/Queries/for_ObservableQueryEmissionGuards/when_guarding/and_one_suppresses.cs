// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryEmissionGuards.when_guarding;

public class and_one_suppresses : given.all_dependencies
{
    ObservableQueryEmissionVerdict _result;

    void Establish()
    {
        _second.Verdict = ObservableQueryEmissionVerdict.Suppress;
        DiscoverGuards(typeof(FirstGuard), typeof(SecondGuard));
    }

    async Task Because() => _result = await _guards.Guard(_context);

    [Fact] void should_suppress_the_emission() => _result.ShouldEqual(ObservableQueryEmissionVerdict.Suppress);
    [Fact] void should_still_ask_every_guard() => (_first.Calls.Count + _second.Calls.Count).ShouldEqual(2);
}
