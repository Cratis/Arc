// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Http.for_AspNetCoreEndpointMapper.when_mapping_query_endpoints.given;

public class a_query_endpoint : Specification
{
    protected WebApplication _app;
    protected AspNetCoreEndpointMapper _mapper;
    protected IEndpointRouteBuilder _routeBuilder;
    protected IQueryPerformerProviders _queryPerformerProviders;

    protected record TestReadModel(string Name);

    void Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.Configure<ArcOptions>(o =>
        {
            o.GeneratedApis.SegmentsToSkipForRoute = 0;
            o.GeneratedApis.IncludeQueryNameInRoute = true;
        });

        var queryPerformer = Substitute.For<IQueryPerformer>();
        queryPerformer.Name.Returns(new QueryName("AllOrders"));
        queryPerformer.FullyQualifiedName.Returns(new FullyQualifiedQueryName("Features.Orders.AllOrders"));
        queryPerformer.Location.Returns(["Features", "Orders"]);
        queryPerformer.Type.Returns(typeof(TestReadModel));
        queryPerformer.ReadModelType.Returns(typeof(TestReadModel));
        queryPerformer.Dependencies.Returns([]);
        queryPerformer.Parameters.Returns(QueryParameters.Empty);
        queryPerformer.AllowsAnonymousAccess.Returns(false);

        _queryPerformerProviders = Substitute.For<IQueryPerformerProviders>();
        _queryPerformerProviders.Performers.Returns([queryPerformer]);
        builder.Services.AddSingleton(_queryPerformerProviders);

        _app = builder.Build();
        _routeBuilder = _app;
        _mapper = new AspNetCoreEndpointMapper(_routeBuilder);
    }

    protected IReadOnlyList<Endpoint> GetEndpoints()
    {
        return _routeBuilder.DataSources
            .SelectMany(ds => ds.Endpoints)
            .ToList();
    }

    protected IEnumerable<RouteEndpoint> GetRouteEndpoints()
    {
        return GetEndpoints().OfType<RouteEndpoint>();
    }

    protected RouteEndpoint? FindEndpointByName(string name)
    {
        return GetRouteEndpoints()
            .FirstOrDefault(e => e.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == name);
    }

    void Destroy()
    {
        _app?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
