// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.for_ArcBuilderExtensions.when_resolving_the_unit_of_work_manager;

[Collection("UsesCurrentDirectory")]
public class and_an_explicit_manager_is_registered_before_chronicle : given.a_chronicle_host
{
    IUnitOfWorkManager _explicitManager;
    IUnitOfWorkManager _resolvedManager;
    ServiceDescriptor _registration;

    void Establish()
    {
        _explicitManager = Substitute.For<IUnitOfWorkManager>();
        var builder = ArcApplication.CreateBuilder();
        builder.AddCratisArc(configureBuilder: arc =>
        {
            arc.Services.AddSingleton(_explicitManager);
            _registration = arc.Services[^1];
            ConfigureChronicle(arc);
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
    [Fact] void should_preserve_the_explicit_registration() => _services.Where(_ => _.ServiceType == typeof(IUnitOfWorkManager)).ShouldContainOnly(_registration);
}
