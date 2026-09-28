// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_handling_a_query;

public class with_a_custom_pipeline_and_no_authorization_declarations : given.a_query_request
{
    int? _status;
    QueryResult? _result;

    void Establish()
    {
        var old = _context.RequestServices;
        _queryPipeline.Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>())
            .Returns(QueryResult.Success(CorrelationId.New()));
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(_queryPipeline)
            .AddSingleton(_observableQueryHandler)
            .AddSingleton(old.GetRequiredService<ICorrelationIdAccessor>())
            .AddSingleton(Options.Create(_arcOptions))
            .BuildServiceProvider();
        _context.RequestServices.Returns(services);
        _context.When(context => context.SetStatusCode(Arg.Any<int>())).Do(call => _status = call.Arg<int>());
        _context.WriteResponseAsJson(Arg.Any<object?>(), typeof(QueryResult), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _result = call.ArgAt<object>(0) as QueryResult;
                return Task.CompletedTask;
            });
    }

    async Task Because() => await _mapper.HandlerFor("GET")(_context);

    [Fact] void should_report_forbidden() => _status.ShouldEqual(403);
    [Fact] void should_report_unauthorized_without_an_error() => _result!.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_report_an_exception() => _result!.HasExceptions.ShouldBeFalse();
    [Fact] void should_not_invoke_the_custom_pipeline() => _queryPipeline.DidNotReceive().Perform(
        Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>());
}
