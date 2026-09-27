// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Concepts;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_handling_a_query;

public class with_invalid_concept_collection_element : given.a_query_endpoint_mapper
{
    IHttpRequestContext _context;
    IQueryPipeline _queryPipeline;
    int? _statusCode;
    QueryResult _result;

    void Establish()
    {
        var performer = Substitute.For<IQueryPerformer>();
        performer.Name.Returns(new QueryName("ByRates"));
        performer.FullyQualifiedName.Returns(new FullyQualifiedQueryName("Rates.ByRates"));
        performer.ReadModelType.Returns(typeof(RateLookup));
        performer.Location.Returns(["Rates"]);
        performer.Parameters.Returns(new QueryParameters([new QueryParameter("rates", typeof(Rate[]))]));
        _queryPerformerProviders.Performers.Returns([performer]);
        _mapper.MapQueryEndpoints(_serviceProvider);

        _queryPipeline = Substitute.For<IQueryPipeline>();
        var correlationIdAccessor = Substitute.For<ICorrelationIdAccessor>();
        correlationIdAccessor.Current.Returns(CorrelationId.New());
        var requestServices = new ServiceCollection()
            .AddLogging()
            .AddSingleton(_queryPipeline)
            .AddSingleton(correlationIdAccessor)
            .AddSingleton(Options.Create(_arcOptions))
            .BuildServiceProvider();

        _context = Substitute.For<IHttpRequestContext>();
        _context.RequestServices.Returns(requestServices);
        _context.Headers.Returns(new Dictionary<string, string>());
        _context.Query.Returns(new Dictionary<string, string> { ["rates"] = "1,bad" });
        _context.When(c => c.SetStatusCode(Arg.Any<int>())).Do(call => _statusCode = call.Arg<int>());
        _context.When(c => c.WriteResponseAsJson(Arg.Any<object>(), typeof(QueryResult), Arg.Any<CancellationToken>()))
            .Do(call => _result = (QueryResult)call.ArgAt<object>(0));
    }

    async Task Because() => await _mapper.HandlerFor("GET")(_context);

    [Fact] void should_return_http_400() => _statusCode.ShouldEqual(400);
    [Fact] void should_report_the_named_validation_failure() => _result.ValidationResults.Single().Members.ShouldContain("rates");
    [Fact] void should_not_report_a_server_error() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_not_invoke_the_query() => _queryPipeline.DidNotReceive().Perform(
        Arg.Any<FullyQualifiedQueryName>(),
        Arg.Any<QueryArguments>(),
        Arg.Any<Paging>(),
        Arg.Any<Sorting>(),
        Arg.Any<IServiceProvider>());

    public record Rate(decimal Value) : ConceptAs<decimal>(Value);

    public record RateLookup;
}
