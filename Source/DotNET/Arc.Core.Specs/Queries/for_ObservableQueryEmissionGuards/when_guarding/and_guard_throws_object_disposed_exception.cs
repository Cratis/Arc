// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ObservableQueryEmissionGuards.when_guarding;

public class and_guard_throws_object_disposed_exception : given.all_dependencies
{
    ObservableQueryEmissionVerdict _result;
    Exception _error;

    void Establish()
    {
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _first.Failure = new ObjectDisposedException(nameof(FirstGuard));
        DiscoverGuards(typeof(FirstGuard), typeof(SecondGuard));
    }

    async Task Because() => _error = await Catch.Exception(AskTheGuards);

    [Fact] void should_fail_closed() => _result.ShouldEqual(ObservableQueryEmissionVerdict.DenyAndTerminate);
    [Fact] void should_not_let_the_failure_escape() => _error.ShouldBeNull();
    [Fact] void should_have_invoked_the_guard() => _first.Calls.ShouldContainOnly(_context);
    [Fact] void should_not_ask_any_later_guard() => _second.Calls.ShouldBeEmpty();
    [Fact] void should_report_it_as_a_guard_failure() => LoggedLevels.ShouldContain(LogLevel.Error);

    IEnumerable<LogLevel> LoggedLevels =>
        _logger.ReceivedCalls()
            .Where(_ => _.GetMethodInfo().Name == nameof(ILogger.Log))
            .Select(_ => (LogLevel)_.GetArguments()[0]!);

    async Task AskTheGuards() => _result = await _guards.Guard(_context);
}
