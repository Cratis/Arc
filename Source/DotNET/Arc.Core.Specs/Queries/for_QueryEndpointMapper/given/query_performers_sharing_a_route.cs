// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.given;

public class query_performers_sharing_a_route : Specification
{
    protected const string Route = "/api/feature/watch-all";

    protected a_recording_endpoint_mapper _mapper;
    protected a_recording_logger _logger;
    protected IServiceProvider _serviceProvider;
    protected IQueryPerformerProviders _queryPerformerProviders;

    void Establish()
    {
        _mapper = new a_recording_endpoint_mapper();
        _logger = new a_recording_logger();
        _queryPerformerProviders = Substitute.For<IQueryPerformerProviders>();

        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(new ArcOptions()));
        services.AddSingleton(_queryPerformerProviders);
        services.AddSingleton<IInstancesOf<IQueryRequestReader>>(new KnownInstancesOf<IQueryRequestReader>([new QueryStringQueryRequestReader()]));
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(_logger));
        _serviceProvider = services.BuildServiceProvider();
    }

    protected static IQueryPerformer PerformerFor<TReadModel>(string fullyQualifiedName)
    {
        var performer = Substitute.For<IQueryPerformer>();
        performer.Name.Returns(new QueryName("WatchAll"));
        performer.FullyQualifiedName.Returns(new FullyQualifiedQueryName(fullyQualifiedName));
        performer.ReadModelType.Returns(typeof(TReadModel));
        performer.Location.Returns(["Feature"]);
        performer.Parameters.Returns(new QueryParameters([]));
        return performer;
    }
}
