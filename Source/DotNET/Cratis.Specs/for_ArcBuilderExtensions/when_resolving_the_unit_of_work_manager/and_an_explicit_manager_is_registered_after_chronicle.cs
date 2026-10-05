// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Chronicle;
using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.for_ArcBuilderExtensions.when_resolving_the_unit_of_work_manager;

[Collection("UsesCurrentDirectory")]
public class and_an_explicit_manager_is_registered_after_chronicle : given.a_chronicle_web_host
{
    IUnitOfWorkManager _explicitManager;
    IUnitOfWorkManager _resolvedManager;
    IUnitOfWorkManager[] _all;
    IEventStore _eventStore;

    void Establish()
    {
        _explicitManager = Substitute.For<IUnitOfWorkManager>();
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc(configureBuilder: arc =>
        {
            ConfigureChronicle(arc);
            arc.Services.AddSingleton(_explicitManager);
        });
        _host = builder.Build();
    }

    void Because()
    {
        using var scope = _host.Services.CreateScope();
        _resolvedManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
        _eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
        _all = scope.ServiceProvider.GetServices<IUnitOfWorkManager>().ToArray();
    }

    [Fact] void should_resolve_the_explicit_manager_for_a_single_service() => ReferenceEquals(_resolvedManager, _explicitManager).ShouldBeTrue();
    [Fact] void should_resolve_the_event_stores_manager_and_the_explicit_manager_from_all_services() => _all.ShouldContainOnly(_eventStore.UnitOfWorkManager, _explicitManager);
}
