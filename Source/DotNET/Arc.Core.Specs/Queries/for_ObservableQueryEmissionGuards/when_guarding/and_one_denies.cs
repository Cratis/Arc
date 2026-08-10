// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryEmissionGuards.when_guarding;

public class and_one_denies : given.all_dependencies
{
    ObservableQueryEmissionVerdict _result;

    void Establish()
    {
        _first.Verdict = ObservableQueryEmissionVerdict.Suppress;
        _second.Verdict = ObservableQueryEmissionVerdict.DenyAndTerminate;
        DiscoverGuards(typeof(FirstGuard), typeof(SecondGuard));
    }

    async Task Because() => _result = await _guards.Guard(_context);

    [Fact] void should_deny_and_terminate() => _result.ShouldEqual(ObservableQueryEmissionVerdict.DenyAndTerminate);
}
