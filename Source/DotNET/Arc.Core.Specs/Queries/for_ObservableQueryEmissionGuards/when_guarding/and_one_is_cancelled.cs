// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ObservableQueryEmissionGuards.when_guarding;

/// <summary>
/// The context carries the subscription's cancellation token and guard authors are told to observe it, so an ordinary
/// tab close lands inside a guard call as an <see cref="OperationCanceledException"/>. Teardown is not a verdict: the
/// emission is still withheld, but reporting every disconnect as "your authorization guard failed" at Error level
/// would bury the failures that are real.
/// </summary>
public class and_one_is_cancelled : given.all_dependencies
{
    readonly CancellationToken _subscriptionEnded = new(true);

    ObservableQueryEmissionVerdict _result;
    Exception _error;

    void Establish()
    {
        // A bare substitute answers false to IsEnabled, which would make the source-generated log method return
        // before ever reaching Log - and this spec would then pass whether or not the cancellation is logged.
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _context = _context with { CancellationToken = _subscriptionEnded };
        _first.Failure = new OperationCanceledException(_subscriptionEnded);
        DiscoverGuards(typeof(FirstGuard), typeof(SecondGuard));
    }

    async Task Because() => _error = await Catch.Exception(AskTheGuards);

    [Fact] void should_still_fail_closed() => _result.ShouldEqual(ObservableQueryEmissionVerdict.DenyAndTerminate);
    [Fact] void should_not_let_the_cancellation_escape() => _error.ShouldBeNull();
    [Fact] void should_not_ask_any_later_guard() => _second.Calls.ShouldBeEmpty();
    [Fact] void should_not_report_it_as_a_security_failure() => LoggedLevels.ShouldNotContain(LogLevel.Error);

    IEnumerable<LogLevel> LoggedLevels =>
        _logger.ReceivedCalls()
            .Where(_ => _.GetMethodInfo().Name == nameof(ILogger.Log))
            .Select(_ => (LogLevel)_.GetArguments()[0]!);

    async Task AskTheGuards() => _result = await _guards.Guard(_context);
}
