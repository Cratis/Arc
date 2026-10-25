// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Introspection.for_IntrospectionService.when_building_metadata;

public class with_artifacts_in_a_named_endpoint_group : Specification
{
    IReadOnlyList<CommandIntrospectionMetadata> _commands = [];
    IReadOnlyList<QueryIntrospectionMetadata> _queries = [];

    void Establish()
    {
        var commandHandler = Substitute.For<ICommandHandler>();
        commandHandler.Location.Returns(["App", "Orders"]);
        commandHandler.CommandType.Returns(typeof(RebuildIndex));
        commandHandler.AllowsAnonymousAccess.Returns(true);

        var queryPerformer = Substitute.For<IQueryPerformer>();
        queryPerformer.Location.Returns(["App", "Orders"]);
        queryPerformer.Name.Returns(new QueryName("Diagnostics"));
        queryPerformer.FullyQualifiedName.Returns(new FullyQualifiedQueryName("App.Orders.Diagnostics"));
        queryPerformer.Type.Returns(typeof(Diagnostics));
        queryPerformer.ReadModelType.Returns(typeof(object));
        queryPerformer.Parameters.Returns(new QueryParameters([]));
        queryPerformer.AllowsAnonymousAccess.Returns(true);

        var commandProviders = Substitute.For<ICommandHandlerProviders>();
        commandProviders.Handlers.Returns([commandHandler]);

        var queryProviders = Substitute.For<IQueryPerformerProviders>();
        queryProviders.Performers.Returns([queryPerformer]);

        var service = new IntrospectionService(commandProviders, queryProviders, Options.Create(new ApiEndpointOptions()), Options.Create(new ArcOptions()));

        _commands = service.Commands;
        _queries = service.Queries;
    }

    [Fact] void should_omit_the_grouped_command() => _commands.ShouldBeEmpty();
    [Fact] void should_omit_the_grouped_query() => _queries.ShouldBeEmpty();

    [EndpointGroup("internal")]
    public record RebuildIndex;

    [EndpointGroup("internal")]
    public record Diagnostics;
}
