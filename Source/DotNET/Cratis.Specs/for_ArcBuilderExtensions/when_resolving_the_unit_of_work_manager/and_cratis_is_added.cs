// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.for_ArcBuilderExtensions.when_resolving_the_unit_of_work_manager;

[Collection("UsesCurrentDirectory")]
public class and_cratis_is_added : given.a_chronicle_web_host
{
    IUnitOfWorkManager _manager;
    IUnitOfWorkManager _resolvedAgain;
    IEventStore _eventStore;
    IUnitOfWorkManager[] _all;

    void Because()
    {
        using var scope = _host.Services.CreateScope();
        _manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
        _resolvedAgain = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
        _eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
        _all = scope.ServiceProvider.GetServices<IUnitOfWorkManager>().ToArray();
    }

    void Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratis(configureArcBuilder: ConfigureChronicle);
        _services = builder.Services;
        _host = builder.Build();
    }

    [Fact] void should_use_the_event_stores_manager() => ReferenceEquals(_manager, _eventStore.UnitOfWorkManager).ShouldBeTrue();
    [Fact] void should_reuse_the_manager_in_the_scope() => ReferenceEquals(_manager, _resolvedAgain).ShouldBeTrue();
    [Fact] void should_select_the_configured_store() => _eventStore.Name.Value.ShouldEqual(EventStore);
    [Fact] void should_have_one_scoped_manager_registration() => _services.Where(_ => _.ServiceType == typeof(IUnitOfWorkManager)).Select(_ => _.Lifetime).ShouldContainOnly(ServiceLifetime.Scoped);
    [Fact] void should_only_resolve_the_event_stores_manager_from_all_services() => _all.ShouldContainOnly(_eventStore.UnitOfWorkManager);
    [Fact] void should_not_register_the_policy_enum() => _services.Where(_ => _.ServiceType == typeof(UnitOfWorkLifecyclePolicy)).ShouldBeEmpty();
}
