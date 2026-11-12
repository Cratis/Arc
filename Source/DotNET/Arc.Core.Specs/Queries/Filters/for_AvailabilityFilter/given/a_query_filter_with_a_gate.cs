// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.Filters.for_AvailabilityFilter.given;

public class a_query_filter_with_a_gate : Specification
{
    protected IAvailabilityGate _gate;
    protected IQueryPerformerProviders _providers;
    protected IQueryPerformer _performer;
    protected AvailabilityFilter _filter;
    protected ServiceProvider _services;
    protected QueryContext _context;
    protected QueryResult _result;

    void Establish()
    {
        _gate = Substitute.For<IAvailabilityGate>();
        _gate.IsGated(typeof(GatedReadModel)).Returns(true);
        _performer = Substitute.For<IQueryPerformer>();
        _performer.Type.Returns(typeof(GatedReadModel));
        _providers = Substitute.For<IQueryPerformerProviders>();
        _providers.TryGetPerformersFor(Arg.Any<FullyQualifiedQueryName>(), out Arg.Any<IQueryPerformer>()!)
            .Returns(call =>
            {
                call[1] = _performer;
                return true;
            });
        _services = new ServiceCollection().AddSingleton(_gate).BuildServiceProvider();
        _filter = new AvailabilityFilter(_providers);
        _context = new QueryContext("Gated.Query", CorrelationId.New(), Paging.NotPaged, Sorting.None, null, null, _services);
    }

    void Destroy() => _services.Dispose();

    public record GatedReadModel;
}
