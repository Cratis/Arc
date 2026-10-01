// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.for_ArcBuilderExtensions.when_resolving_the_unit_of_work_manager;

[Collection("UsesCurrentDirectory")]
public class and_an_explicit_manager_is_registered_before_chronicle : given.a_chronicle_host
{
    IUnitOfWorkManager _explicitManager;
    IUnitOfWorkManager _factoryManager;
    IUnitOfWorkManager _keyedManager;
    IUnitOfWorkManager _resolvedKeyedManager;
    ServiceDescriptor _instanceDescriptor;
    ServiceDescriptor _factoryDescriptor;
    ServiceDescriptor _keyedDescriptor;
    IUnitOfWorkManager[] _managers;
    IUnitOfWorkManager _resolvedManager;

    void Establish()
    {
        var builder = ArcApplication.CreateBuilder();
        _explicitManager = Substitute.For<IUnitOfWorkManager>();
        _factoryManager = Substitute.For<IUnitOfWorkManager>();
        _keyedManager = Substitute.For<IUnitOfWorkManager>();
        _instanceDescriptor = ServiceDescriptor.Singleton(_explicitManager);
        _factoryDescriptor = ServiceDescriptor.Scoped<IUnitOfWorkManager>(_ => _factoryManager);
        _keyedDescriptor = ServiceDescriptor.KeyedScoped<IUnitOfWorkManager>("custom", (_, _) => _keyedManager);
        builder.Services.Add(_instanceDescriptor);
        builder.Services.Add(_factoryDescriptor);
        builder.Services.Add(_keyedDescriptor);
        builder.AddCratisArc(configureBuilder: ConfigureChronicle);
        _services = builder.Services;
        _host = builder.Build();
    }

    async Task Because()
    {
        await using var scope = _host.Services.CreateAsyncScope();
        _managers = scope.ServiceProvider.GetServices<IUnitOfWorkManager>().ToArray();
        _resolvedManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
        _resolvedKeyedManager = scope.ServiceProvider.GetRequiredKeyedService<IUnitOfWorkManager>("custom");
    }

    [Fact] void should_keep_the_explicit_instance_descriptor() => _services.ShouldContain(_instanceDescriptor);
    [Fact] void should_keep_the_explicit_factory_descriptor() => _services.ShouldContain(_factoryDescriptor);
    [Fact] void should_keep_the_keyed_descriptor() => _services.ShouldContain(_keyedDescriptor);
    [Fact] void should_resolve_the_explicit_instance() => _managers.ShouldContain(_explicitManager);
    [Fact] void should_resolve_the_explicit_factory() => _managers.ShouldContain(_factoryManager);
    [Fact] void should_resolve_the_keyed_manager() => ReferenceEquals(_resolvedKeyedManager, _keyedManager).ShouldBeTrue();
    [Fact] void should_use_the_last_explicit_manager_by_default() => ReferenceEquals(_resolvedManager, _factoryManager).ShouldBeTrue();
    [Fact] void should_not_add_a_default_manager() => _managers.ShouldContainOnly(_explicitManager, _factoryManager);
    [Fact] void should_remove_the_convention_manager_descriptor() => _services.Where(_ => _.ServiceType == typeof(IUnitOfWorkManager) && !_.IsKeyedService && _.ImplementationType == typeof(UnitOfWorkManager)).ShouldBeEmpty();
}
