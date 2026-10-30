// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Introspection.for_IntrospectionService.when_building_metadata;

public class with_a_query_method_in_a_named_endpoint_group : Specification
{
    IReadOnlyList<QueryIntrospectionMetadata> _queries;

    void Because()
    {
        var performer = Substitute.For<IQueryPerformer>();
        performer.Type.Returns(typeof(Model));
        performer.ReadModelType.Returns(typeof(Model));
        performer.Location.Returns(["App", "Queries"]);
        performer.Name.Returns(new QueryName(nameof(Model.Query)));
        performer.FullyQualifiedName.Returns(new FullyQualifiedQueryName("App.Queries.Query"));
        performer.Parameters.Returns(new QueryParameters([]));
        var queries = Substitute.For<IQueryPerformerProviders>();
        queries.Performers.Returns([performer]);
        var commands = Substitute.For<ICommandHandlerProviders>();
        commands.Handlers.Returns([]);
        _queries = new IntrospectionService(commands, queries, Options.Create(new ApiEndpointOptions()), Options.Create(new ArcOptions())).Queries;
    }

    [Fact] void should_omit_the_grouped_query() => _queries.ShouldBeEmpty();

    public record Model
    {
        [EndpointGroup("internal")]
        public static int Query() => 0;
    }
}
