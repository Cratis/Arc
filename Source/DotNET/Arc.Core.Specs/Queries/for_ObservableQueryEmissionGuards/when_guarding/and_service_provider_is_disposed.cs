// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ObservableQueryEmissionGuards.when_guarding;

public class and_service_provider_is_disposed : given.all_dependencies
{
    readonly CancellationToken _subscriptionEnded = new(true);

    ObservableQueryEmissionVerdict _result;
    Exception _error;

    void Establish()
    {
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _context = _context with { CancellationToken = _subscriptionEnded };
        ((IDisposable)_context.ServiceProvider).Dispose();
        DiscoverGuards(typeof(FirstGuard), typeof(SecondGuard));
    }

    async Task Because() => _error = await Catch.Exception(AskTheGuards);

    [Fact] void should_fail_closed() => _result.ShouldEqual(ObservableQueryEmissionVerdict.DenyAndTerminate);
    [Fact] void should_not_let_the_failure_escape() => _error.ShouldBeNull();
    [Fact] void should_not_invoke_the_guard() => _first.Calls.ShouldBeEmpty();
    [Fact] void should_not_ask_any_later_guard() => _second.Calls.ShouldBeEmpty();
    [Fact] void should_not_report_it_as_a_guard_failure() => LoggedLevels.ShouldNotContain(LogLevel.Error);

    IEnumerable<LogLevel> LoggedLevels =>
        _logger.ReceivedCalls()
            .Where(_ => _.GetMethodInfo().Name == nameof(ILogger.Log))
            .Select(_ => (LogLevel)_.GetArguments()[0]!);

    async Task AskTheGuards() => _result = await _guards.Guard(_context);
}
