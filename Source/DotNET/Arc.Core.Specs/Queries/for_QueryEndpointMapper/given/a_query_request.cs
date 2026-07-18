// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.given;

public class a_query_request : a_query_endpoint_mapper
{
    protected IHttpRequestContext _context;
    protected IQueryPipeline _queryPipeline;
    protected IObservableQueryHandler _observableQueryHandler;
    protected int? _statusCode;

    protected record TestReadModel;

    void Establish()
    {
        var performer = Substitute.For<IQueryPerformer>();
        performer.Name.Returns(new QueryName("AllOrders"));
        performer.FullyQualifiedName.Returns(new FullyQualifiedQueryName("Features.Orders.AllOrders"));
        performer.ReadModelType.Returns(typeof(TestReadModel));
        performer.Location.Returns(["Features", "Orders"]);
        performer.AllowsAnonymousAccess.Returns(false);
        performer.Parameters.Returns(new QueryParameters([]));
        _queryPerformerProviders.Performers.Returns([performer]);

        _mapper.MapQueryEndpoints(_serviceProvider);

        _queryPipeline = Substitute.For<IQueryPipeline>();
        _observableQueryHandler = Substitute.For<IObservableQueryHandler>();
        _observableQueryHandler.IsStreamingResult(Arg.Any<object?>()).Returns(false);

        var correlationIdAccessor = Substitute.For<ICorrelationIdAccessor>();
        correlationIdAccessor.Current.Returns(CorrelationId.New());

        var requestServices = new ServiceCollection()
            .AddLogging()
            .AddSingleton(_queryPipeline)
            .AddSingleton(_observableQueryHandler)
            .AddSingleton(correlationIdAccessor)
            .AddSingleton(Options.Create(_arcOptions))
            .BuildServiceProvider();

        _context = Substitute.For<IHttpRequestContext>();
        _context.RequestServices.Returns(requestServices);
        _context.Headers.Returns(new Dictionary<string, string>());
        _context.Query.Returns(new Dictionary<string, string>());
        _context.When(c => c.SetStatusCode(Arg.Any<int>())).Do(ci => _statusCode = ci.Arg<int>());
    }
}
