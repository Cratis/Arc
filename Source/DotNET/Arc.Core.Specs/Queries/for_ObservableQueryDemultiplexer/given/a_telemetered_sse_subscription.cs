// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Observability;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.given;

/// <summary>
/// An SSE connection to a hub that reports its subscriptions and pushes through a recorded meter and activity source.
/// </summary>
public class a_telemetered_sse_subscription : an_sse_connection_owned_by_a_caller
{
    internal ObservedQueryServices _observed;

    void Establish()
    {
        _observed = new ObservedQueryServices([QueryName]);

        var services = new ServiceCollection().AddSingleton(_fallbackAuthorization);
        services.AddSingleton((IQueryPerformerProviders)_observed.Services.GetService(typeof(IQueryPerformerProviders))!);
        services.AddSingleton((PipelineMetrics)_observed.Services.GetService(typeof(PipelineMetrics))!);
        services.AddSingleton((IActivitySource<QueryPipeline>)_observed.Services.GetService(typeof(IActivitySource<QueryPipeline>))!);
        UseServiceProvider(services.BuildServiceProvider());
    }

    void Destroy() => _observed.Dispose();
}
