// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;
using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.for_ArcBuilderExtensions.when_resolving_the_unit_of_work_manager;

[Collection("UsesCurrentDirectory")]
public class and_chronicle_is_registered_first_in_a_web_host : given.a_chronicle_host
{
    IUnitOfWorkManager _manager;
    IUnitOfWorkManager _resolvedAgain;
    IUnitOfWorkManager _otherManager;
    IEventStore _eventStore;
    IEventStore _otherEventStore;
    Exception? _lateStaging;

    void Establish()
    {
        var builder = WebApplication.CreateBuilder();
        ConfigureChronicle(new ArcBuilder(builder, TypesServiceCollectionExtensions.CurrentTypeUniverse()));
        builder.AddCratisArc();
        _services = builder.Services;
        _host = builder.Build();
    }

    async Task Because()
    {
        var tenants = _host.Services.GetRequiredService<ITenantScope>();
        using (tenants.Begin("tenant-a"))
        await using (var scope = _host.Services.CreateAsyncScope())
        {
            _manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            _resolvedAgain = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            _eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
            using var unit = _manager.Begin(CorrelationId.New());
            await unit.Rollback();
            _lateStaging = Catch.Exception(() => unit.AddEvents(EventSequenceId.Log, [], []));
        }
        using (tenants.Begin("tenant-b"))
        await using (var scope = _host.Services.CreateAsyncScope())
        {
            _otherManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            _otherEventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
        }
    }

    [Fact] void should_use_the_event_stores_manager() => ReferenceEquals(_manager, _eventStore.UnitOfWorkManager).ShouldBeTrue();
    [Fact] void should_reuse_the_manager_in_the_scope() => ReferenceEquals(_manager, _resolvedAgain).ShouldBeTrue();
    [Fact] void should_select_the_configured_store() => _eventStore.Name.Value.ShouldEqual(EventStore);
    [Fact] void should_select_the_active_namespace() => _eventStore.Namespace.Value.ShouldEqual("tenant-a");
    [Fact] void should_select_the_other_namespace() => _otherEventStore.Namespace.Value.ShouldEqual("tenant-b");
    [Fact] void should_use_the_other_event_stores_manager() => ReferenceEquals(_otherManager, _otherEventStore.UnitOfWorkManager).ShouldBeTrue();
    [Fact] void should_not_share_managers_between_tenants() => ReferenceEquals(_manager, _otherManager).ShouldBeFalse();
    [Fact] void should_honor_the_strict_policy() => _lateStaging.ShouldBeOfExactType<UnitOfWorkIsCompleted>();
    [Fact] void should_have_one_scoped_manager_registration() => _services.Where(_ => _.ServiceType == typeof(IUnitOfWorkManager)).Select(_ => _.Lifetime).ShouldContainOnly(ServiceLifetime.Scoped);
    [Fact] void should_not_register_the_policy_enum() => _services.Where(_ => _.ServiceType == typeof(UnitOfWorkLifecyclePolicy)).ShouldBeEmpty();
}
