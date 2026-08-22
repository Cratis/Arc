// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ObservableQueryEmissionGuards.when_guarding;

public class and_guard_activation_throws_object_disposed_exception : given.all_dependencies
{
    ServiceProvider _serviceProvider;
    ObservableQueryEmissionVerdict _result;
    Exception _error;

    void Establish()
    {
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        var services = new ServiceCollection();
        services.AddTransient<ActivationFailureDependency>(_ => throw new ObjectDisposedException(nameof(ActivationFailureDependency)));
        _serviceProvider = services.BuildServiceProvider();
        _context = _context with { ServiceProvider = _serviceProvider };
        DiscoverGuards(typeof(ActivationFailureGuard));
    }

    async Task Because() => _error = await Catch.Exception(AskTheGuards);

    void Destroy() => _serviceProvider.Dispose();

    [Fact] void should_fail_closed() => _result.ShouldEqual(ObservableQueryEmissionVerdict.DenyAndTerminate);
    [Fact] void should_not_let_the_failure_escape() => _error.ShouldBeNull();
    [Fact] void should_report_it_as_a_guard_failure() => LoggedLevels.ShouldContain(LogLevel.Error);

    IEnumerable<LogLevel> LoggedLevels =>
        _logger.ReceivedCalls()
            .Where(_ => _.GetMethodInfo().Name == nameof(ILogger.Log))
            .Select(_ => (LogLevel)_.GetArguments()[0]!);

    async Task AskTheGuards() => _result = await _guards.Guard(_context);

    public class ActivationFailureGuard(ActivationFailureDependency dependency) : IGuardObservableQueryEmission
    {
        public Task<ObservableQueryEmissionVerdict> Guard(ObservableQueryEmissionContext context) =>
            Task.FromResult(ObservableQueryEmissionVerdict.Allow);
    }

    public class ActivationFailureDependency;
}
