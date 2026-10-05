// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Chronicle;
using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.for_ArcBuilderExtensions.when_resolving_the_unit_of_work_manager;

[Collection("UsesCurrentDirectory")]
public class and_an_explicit_manager_is_registered_in_the_chronicle_callback : given.a_chronicle_web_host
{
    IUnitOfWorkManager _explicitManager;
    IUnitOfWorkManager _resolvedManager;
    ServiceDescriptor _registration;

    void Establish()
    {
        _explicitManager = Substitute.For<IUnitOfWorkManager>();
        var builder = WebApplication.CreateBuilder();
        builder.AddCratis(configureArcBuilder: arc =>
        {
            arc.Services.AddSingleton(Substitute.For<Cratis.Chronicle.Connections.IChronicleConnection>());
            arc.WithChronicle(
                options =>
                {
                    options.EventStore = EventStore;
                    options.AutoDiscoverAndRegister = false;
                },
                chronicle =>
                {
                    chronicle.WithArtifactsProvider(Substitute.For<IClientArtifactsProvider>());
                    chronicle.Services.AddSingleton(_explicitManager);
                    _registration = chronicle.Services[^1];
                });
        });
        _services = builder.Services;
        _host = builder.Build();
    }

    void Because()
    {
        using var scope = _host.Services.CreateScope();
        _resolvedManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
    }

    [Fact] void should_resolve_the_explicit_manager() => ReferenceEquals(_resolvedManager, _explicitManager).ShouldBeTrue();
    [Fact] void should_preserve_the_singleton_lifetime() => _registration.Lifetime.ShouldEqual(ServiceLifetime.Singleton);
    [Fact] void should_preserve_the_registration() => _services.Where(_ => _.ServiceType == typeof(IUnitOfWorkManager)).ShouldContain(_registration);
}
