// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.for_ArcBuilderExtensions.when_resolving_the_unit_of_work_manager;

[Collection("UsesCurrentDirectory")]
public class and_an_explicit_transient_manager_is_registered_before_adding_cratis : given.a_chronicle_web_host
{
    ServiceDescriptor _registration;
    IUnitOfWorkManager _resolvedManager;

    void Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddTransient<IUnitOfWorkManager, UnitOfWorkManager>();
        _registration = builder.Services[^1];
        builder.AddCratis(configureArcBuilder: ConfigureChronicle);
        _services = builder.Services;
        _host = builder.Build();
    }

    void Because()
    {
        using var scope = _host.Services.CreateScope();
        _resolvedManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
    }

    [Fact] void should_resolve_the_explicit_implementation() => _resolvedManager.ShouldBeOfExactType<UnitOfWorkManager>();
    [Fact] void should_preserve_the_transient_lifetime() => _services.Single(_ => _.ServiceType == typeof(IUnitOfWorkManager)).Lifetime.ShouldEqual(ServiceLifetime.Transient);
    [Fact] void should_preserve_the_implementation_type() => _services.Single(_ => _.ServiceType == typeof(IUnitOfWorkManager)).ImplementationType.ShouldEqual(typeof(UnitOfWorkManager));
    [Fact] void should_preserve_only_the_explicit_registration() => _services.Where(_ => _.ServiceType == typeof(IUnitOfWorkManager)).ShouldContainOnly(_registration);
}
