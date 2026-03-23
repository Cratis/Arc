// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.given;

public class a_query_endpoint_mapper : Specification
{
    protected IEndpointMapper _mapper;
    protected IServiceProvider _serviceProvider;
    protected IQueryPerformerProviders _queryPerformerProviders;

    void Establish()
    {
        _mapper = Substitute.For<IEndpointMapper>();
        _queryPerformerProviders = Substitute.For<IQueryPerformerProviders>();

        var arcOptions = new ArcOptions();
        var optionsWrapper = Options.Create(arcOptions);

        var services = new ServiceCollection();
        services.AddSingleton(optionsWrapper);
        services.AddSingleton(_queryPerformerProviders);
        _serviceProvider = services.BuildServiceProvider();
    }
}
