// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryEmissionGuards.when_guarding;

public class and_one_throws : given.all_dependencies
{
    ObservableQueryEmissionVerdict _result;
    Exception _error;

    void Establish()
    {
        _first.Failure = new TimeoutException("the session store did not answer");
        DiscoverGuards(typeof(FirstGuard), typeof(SecondGuard));
    }

    async Task Because() => _error = await Catch.Exception(AskTheGuards);

    [Fact] void should_fail_closed() => _result.ShouldEqual(ObservableQueryEmissionVerdict.DenyAndTerminate);
    [Fact] void should_not_let_the_failure_escape() => _error.ShouldBeNull();
    [Fact] void should_not_ask_any_later_guard() => _second.Calls.ShouldBeEmpty();

    async Task AskTheGuards() => _result = await _guards.Guard(_context);
}
