// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_a_traced_tenant : given.a_traced_query_pipeline
{
    TenantIdAccessor _tenants;
    readonly TenantId _tenant = new("acme");

    void Establish()
    {
        WithKnownQuery();
        _tenants = new(Substitute.For<ITenantIdResolver>());
        _serviceProvider.GetService(typeof(TenantIdAccessor)).Returns(_tenants);
        var data = new List<object> { new() };
        _queryPerformer.Perform(Arg.Any<QueryContext>()).Returns(ValueTask.FromResult<object?>(data));
        _queryRenderers.Render(_queryName, data, _serviceProvider).Returns(new QueryRendererResult(1, data));
    }

    async Task Because()
    {
        using var tenant = _tenants.Begin(_tenant);
        await Perform();
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_legacy_tenant() => QuerySpan.GetTagItem(WellKnownTelemetryNames.Tenant).ShouldEqual(_tenant.Value);
    [Fact] void should_add_the_aligned_tenant() => QuerySpan.GetTagItem(WellKnownTelemetryNames.TenantId).ShouldEqual(_tenant.Value);
}
