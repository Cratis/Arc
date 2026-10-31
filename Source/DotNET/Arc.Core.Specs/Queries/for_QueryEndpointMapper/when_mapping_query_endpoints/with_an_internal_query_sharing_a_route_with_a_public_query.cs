// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_mapping_query_endpoints;

public class with_an_internal_query_sharing_a_route_with_a_public_query : given.query_performers_sharing_a_route
{
    public record PublicReadModel;
    record InternalReadModel;

    void Establish()
    {
        IQueryPerformer[] performers =
        [
            PerformerFor<InternalReadModel>("Feature.InternalReadModel.WatchAll"),
            PerformerFor<PublicReadModel>("Feature.PublicReadModel.WatchAll")
        ];
        _queryPerformerProviders.Performers.Returns(performers);
    }

    void Because() => _mapper.MapQueryEndpoints(_serviceProvider);

    [Fact] void should_map_only_one_endpoint() => _mapper.CountFor("GET").ShouldEqual(1);
    [Fact] void should_map_the_public_query() => _mapper.Mapped.Single().Metadata!.Name.ShouldContain("Feature.PublicReadModel.WatchAll");
    [Fact] void should_not_warn() => _logger.Entries.Exists(_ => _.Level == LogLevel.Warning).ShouldBeFalse();
    [Fact] void should_report_the_skipped_internal_query_at_debug_level() =>
        _logger.Entries.Single(_ => _.Level == LogLevel.Debug && _.Message.Contains("Feature.InternalReadModel.WatchAll")).Message.ShouldContain("Feature.PublicReadModel.WatchAll");
}
