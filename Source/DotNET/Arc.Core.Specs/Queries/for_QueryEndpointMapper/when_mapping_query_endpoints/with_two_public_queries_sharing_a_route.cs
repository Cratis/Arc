// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_mapping_query_endpoints;

public class with_two_public_queries_sharing_a_route : given.query_performers_sharing_a_route
{
    public record FirstReadModel;
    public record SecondReadModel;

    (LogLevel Level, string Message) _warning;

    void Establish()
    {
        IQueryPerformer[] performers =
        [
            PerformerFor<FirstReadModel>("Feature.FirstReadModel.WatchAll"),
            PerformerFor<SecondReadModel>("Feature.SecondReadModel.WatchAll")
        ];
        _queryPerformerProviders.Performers.Returns(performers);
    }

    void Because()
    {
        _mapper.MapQueryEndpoints(_serviceProvider);
        _warning = _logger.Entries.Single(_ => _.Level == LogLevel.Warning);
    }

    [Fact] void should_map_only_one_endpoint() => _mapper.CountFor("GET").ShouldEqual(1);
    [Fact] void should_name_the_route() => _warning.Message.ShouldContain(Route);
    [Fact] void should_name_the_mapped_query() => _warning.Message.ShouldContain("Feature.FirstReadModel.WatchAll");
    [Fact] void should_name_the_skipped_query() => _warning.Message.ShouldContain("Feature.SecondReadModel.WatchAll");
}
