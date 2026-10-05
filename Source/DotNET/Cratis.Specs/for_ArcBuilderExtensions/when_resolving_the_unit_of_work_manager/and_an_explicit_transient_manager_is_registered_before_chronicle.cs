// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.for_ArcBuilderExtensions.when_resolving_the_unit_of_work_manager;

[Collection("UsesCurrentDirectory")]
public class and_an_explicit_transient_manager_is_registered_before_chronicle : given.a_chronicle_web_host
{
    ServiceDescriptor _registration;

    void Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc(configureBuilder: arc =>
        {
            arc.Services.AddTransient<IUnitOfWorkManager, UnitOfWorkManager>();
            _registration = arc.Services[^1];
            ConfigureChronicle(arc);
        });
        _services = builder.Services;
        _host = builder.Build();
    }

    [Fact] void should_use_the_explicit_registration() => ReferenceEquals(_services.Last(_ => _.ServiceType == typeof(IUnitOfWorkManager)), _registration).ShouldBeTrue();
    [Fact] void should_preserve_the_transient_lifetime() => _services.Last(_ => _.ServiceType == typeof(IUnitOfWorkManager)).Lifetime.ShouldEqual(ServiceLifetime.Transient);
    [Fact] void should_preserve_the_implementation_type() => _services.Last(_ => _.ServiceType == typeof(IUnitOfWorkManager)).ImplementationType.ShouldEqual(typeof(UnitOfWorkManager));
    [Fact] void should_preserve_only_the_explicit_registration() => _services.Where(_ => _.ServiceType == typeof(IUnitOfWorkManager)).ShouldContainOnly(_registration);
}
