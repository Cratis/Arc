// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Traces;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_validating;

public class and_a_query_is_called_before_the_command : given.a_command_pipeline_and_a_handler_for_command
{
    readonly DateTimeOffset _received = new(2026, 6, 7, 8, 9, 10, TimeSpan.Zero);
    DateTimeOffset? _queryReceipt;
    DateTimeOffset? _commandReceipt;
    QueryPipeline _queryPipeline;
    System.Diagnostics.ActivitySource _queryActivitySource;

    void Establish()
    {
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(_received, _received.AddMinutes(1));
        _serviceProvider.GetService(typeof(TimeProvider)).Returns(clock);
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(call =>
        {
            _commandReceipt = call.Arg<CommandContext>().ReceivedAt;
            return CommandResult.Success(_correlationId);
        });

        var queryFilters = Substitute.For<IQueryFilters>();
        queryFilters.OnPerform(Arg.Any<QueryContext>()).Returns(call =>
        {
            _queryReceipt = call.Arg<QueryContext>().ReceivedAt;
            return QueryResult.Success(_correlationId);
        });
        var performer = Substitute.For<IQueryPerformer>();
        performer.Dependencies.Returns([]);
        var providers = Substitute.For<IQueryPerformerProviders>();
        providers.TryGetPerformersFor(Arg.Any<FullyQualifiedQueryName>(), out var _).Returns(call =>
        {
            call[1] = performer;
            return true;
        });
        var activitySource = Substitute.For<IActivitySource<QueryPipeline>>();
        _queryActivitySource = new System.Diagnostics.ActivitySource("Cratis.Arc.Test");
        activitySource.ActualSource.Returns(_queryActivitySource);
        _queryPipeline = new QueryPipeline(
            _correlationIdAccessor,
            Substitute.For<IQueryContextManager>(),
            queryFilters,
            providers,
            Substitute.For<IQueryRenderers>(),
            Substitute.For<IReadModelInterceptors>(),
            Substitute.For<Cratis.Arc.Validation.IDiscoverableValidators>(),
            activitySource);
    }

    async Task Because()
    {
        using (OperationContextScope.Begin(_serviceProvider))
        {
            using var dispatch = OperationContextScope.ForwardTransportReceipt();
            await _queryPipeline.Perform("Test.Receipt", QueryArguments.Empty, Paging.NotPaged, Sorting.None, _serviceProvider);
            await _commandPipeline.Validate(_command, _serviceProvider);
        }
    }

    void Cleanup() => _queryActivitySource?.Dispose();

    [Fact] void should_keep_the_transport_receipt_in_both_pipelines()
    {
        _queryReceipt.ShouldEqual(_received);
        _commandReceipt.ShouldEqual(_received);
    }
}
