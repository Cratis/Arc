// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSources;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.for_ArcBuilderExtensions.when_resolving_event_sources;

[Collection("UsesCurrentDirectory")]
public class without_manual_registration : given.a_host_with_an_event_source
{
    IEventSources _eventSources;

    void Because() => _eventSources = _scope.ServiceProvider.GetRequiredService<IEventSources>();

    [Fact] void should_resolve_the_event_stores_event_sources() => ReferenceEquals(_eventSources, _eventStore.EventSources).ShouldBeTrue();
    [Fact] void should_resolve_the_declared_event_source() => _eventSources.GetFor(typeof(HostedLedger)).Name.ShouldEqual("hosted-ledger");
    [Fact] void should_register_event_sources_as_scoped() => _services.Last(_ => _.ServiceType == typeof(IEventSources)).Lifetime.ShouldEqual(ServiceLifetime.Scoped);
}
