// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.given;

public class a_query_endpoint_mapper : Specification
{
    protected a_recording_endpoint_mapper _mapper;
    protected IServiceProvider _serviceProvider;
    protected IQueryPerformerProviders _queryPerformerProviders;
    protected ArcOptions _arcOptions;

    void Establish()
    {
        _mapper = new a_recording_endpoint_mapper();
        _queryPerformerProviders = Substitute.For<IQueryPerformerProviders>();

        _arcOptions = new ArcOptions();
        var optionsWrapper = Options.Create(_arcOptions);

        var readers = new KnownInstancesOf<IQueryRequestReader>(
            [new QueryStringQueryRequestReader(), new BodyQueryRequestReader()]);

        var services = new ServiceCollection();
        services.AddSingleton(optionsWrapper);
        services.AddSingleton(_queryPerformerProviders);
        services.AddSingleton<IInstancesOf<IQueryRequestReader>>(readers);
        _serviceProvider = services.BuildServiceProvider();
    }
}
