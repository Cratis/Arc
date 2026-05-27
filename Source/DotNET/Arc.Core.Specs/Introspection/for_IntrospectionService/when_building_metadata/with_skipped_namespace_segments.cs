// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Introspection.for_IntrospectionService.when_building_metadata;

public class with_skipped_namespace_segments : Specification
{
    IReadOnlyList<CommandIntrospectionMetadata> _commands = [];
    IReadOnlyList<QueryIntrospectionMetadata> _queries = [];

    void Establish()
    {
        var commandHandler = Substitute.For<ICommandHandler>();
        commandHandler.Location.Returns(["Cratis", "Arc", "Features", "Orders"]);
        commandHandler.CommandType.Returns(typeof(CreateOrder));
        commandHandler.AllowsAnonymousAccess.Returns(true);

        var queryPerformer = Substitute.For<IQueryPerformer>();
        queryPerformer.Location.Returns(["Cratis", "Arc", "Features", "Orders"]);
        queryPerformer.Name.Returns(new QueryName("ListOrders"));
        queryPerformer.FullyQualifiedName.Returns(new FullyQualifiedQueryName("Cratis.Arc.Features.Orders.ListOrders"));
        queryPerformer.Type.Returns(typeof(ListOrders));
        queryPerformer.ReadModelType.Returns(typeof(object));
        queryPerformer.Parameters.Returns(QueryParameters.Empty);
        queryPerformer.AllowsAnonymousAccess.Returns(true);
        queryPerformer.SupportsPaging.Returns(false);

        var commandProviders = Substitute.For<ICommandHandlerProviders>();
        commandProviders.Handlers.Returns([commandHandler]);

        var queryProviders = Substitute.For<IQueryPerformerProviders>();
        queryProviders.Performers.Returns([queryPerformer]);

        var options = Options.Create(new ApiEndpointOptions
        {
            RoutePrefix = "api",
            SegmentsToSkipForRoute = 2,
            IncludeCommandNameInRoute = true,
            IncludeQueryNameInRoute = false,
        });

        var service = new IntrospectionService(commandProviders, queryProviders, options);

        _commands = service.Commands;
        _queries = service.Queries;
    }

    [Fact] void should_build_command_route_using_skipped_namespace_segments() => _commands.Single().Route.ShouldEqual("/api/features/orders/create-order");
    [Fact] void should_build_query_route_using_skipped_namespace_segments() => _queries.Single().Route.ShouldEqual("/api/features/orders");

    public record CreateOrder;
    public record ListOrders;
}