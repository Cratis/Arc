// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Execution;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Commands.for_CommandFilters.when_executing_filters;

public class and_authorization_configuration_is_invalid : Specification
{
    CommandResult _result;
    ICommandFilter _throwingFilter;
    ICommandFilter _laterFilter;
    CommandContext _context;
    ILogger<CommandFilters> _logger;
    System.Diagnostics.ActivitySource _activitySource;

    void Establish()
    {
        _logger = Substitute.For<ILogger<CommandFilters>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        var services = new ServiceCollection().AddSingleton(_logger).BuildServiceProvider();
        _context = new CommandContext(CorrelationId.New(), typeof(object), new object(), [], new(), ServiceProvider: services);
        _throwingFilter = Substitute.For<ICommandFilter>();
        _laterFilter = Substitute.For<ICommandFilter>();
        _throwingFilter.OnExecution(_context).Returns<Task<CommandResult>>(_ => throw new InvalidAuthorizationConfiguration("Authorization evaluation is unavailable."));
        var source = Substitute.For<IActivitySource<CommandFilters>>();
        _activitySource = new System.Diagnostics.ActivitySource("Cratis.Arc.Test");
        source.ActualSource.Returns(_activitySource);
        _filters = new CommandFilters(new KnownInstancesOf<ICommandFilter>([_throwingFilter, _laterFilter]), source);
    }

    CommandFilters _filters;

    void Cleanup() => _activitySource.Dispose();

    async Task Because() => _result = await _filters.OnExecution(_context);

    [Fact] void should_report_unauthorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_report_an_error() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_not_run_later_filters() => _laterFilter.DidNotReceive().OnExecution(_context);
    [Fact] void should_log_the_configuration_failure() => _logger.ReceivedCalls()
        .Any(call => call.GetMethodInfo().Name == nameof(ILogger.Log) &&
            call.GetArguments()[0] is LogLevel.Error &&
            call.GetArguments()[3] is InvalidAuthorizationConfiguration)
        .ShouldBeTrue();
}
