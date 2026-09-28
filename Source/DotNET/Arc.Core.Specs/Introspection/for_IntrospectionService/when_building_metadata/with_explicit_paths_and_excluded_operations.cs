// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Introspection.for_IntrospectionService.when_building_metadata;

public class with_explicit_paths_and_excluded_operations : Specification
{
    IReadOnlyList<CommandIntrospectionMetadata> _commands;
    IReadOnlyList<QueryIntrospectionMetadata> _queries;

    void Because()
    {
        var commandProviders = Substitute.For<ICommandHandlerProviders>();
        ICommandHandler[] handlers = [Command(typeof(StableCommand)), Command(typeof(HiddenCommand))];
        commandProviders.Handlers.Returns(handlers);
        var queryProviders = Substitute.For<IQueryPerformerProviders>();
        IQueryPerformer[] performers = [
            Query(typeof(VisibleQueries), nameof(VisibleQueries.Find), "/catalog/find"),
            Query(typeof(VisibleQueries), nameof(VisibleQueries.Hidden), "/catalog/hidden"),
            Query(typeof(HiddenQueries), nameof(HiddenQueries.Find), "/catalog/private")
        ];
        queryProviders.Performers.Returns(performers);
        var service = new IntrospectionService(
            commandProviders,
            queryProviders,
            Options.Create(new ApiEndpointOptions()),
            Options.Create(new ArcOptions()));
        _commands = service.Commands;
        _queries = service.Queries;
    }

    [Fact] void should_use_the_explicit_command_path() => _commands.Single().Route.ShouldEqual("/orders/stable");
    [Fact] void should_omit_marked_commands() => _commands.Count.ShouldEqual(1);
    [Fact] void should_use_the_explicit_query_path() => _queries.Single().Route.ShouldEqual("/catalog/find");
    [Fact] void should_omit_marked_query_methods_and_types() => _queries.Count.ShouldEqual(1);

    static ICommandHandler Command(Type type)
    {
        var handler = Substitute.For<ICommandHandler>();
        handler.CommandType.Returns(type);
        handler.Location.Returns(["App", "Orders"]);
        return handler;
    }

    static IQueryPerformer Query(Type type, string method, string path)
    {
        var performer = Substitute.For<IQueryPerformer>();
        performer.Name.Returns(new QueryName(method));
        performer.Type.Returns(type);
        performer.ReadModelType.Returns(type);
        performer.Location.Returns(["App", "Orders"]);
        performer.FullyQualifiedName.Returns(new FullyQualifiedQueryName($"{type.FullName}.{method}"));
        performer.CustomRoute.Returns(path);
        performer.Parameters.Returns(new QueryParameters([]));
        return performer;
    }

    [Path("/orders/stable")]
    public record StableCommand;

    [ExcludeFromDiscovery]
    public record HiddenCommand;

    public static class VisibleQueries
    {
        public static int Find() => 1;
        [ExcludeFromDiscovery]
        public static int Hidden() => 2;
    }

    [ExcludeFromDiscovery]
    public static class HiddenQueries
    {
        public static int Find() => 3;
    }
}
