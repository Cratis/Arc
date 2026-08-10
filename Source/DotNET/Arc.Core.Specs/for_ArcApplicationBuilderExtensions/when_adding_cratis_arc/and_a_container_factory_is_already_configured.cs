// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_ArcApplicationBuilderExtensions.when_adding_cratis_arc;

/// <summary>
/// An application wiring its own container — Autofac, Lamar, or a factory of its own — states that by calling
/// <see cref="ArcApplicationBuilder.ConfigureContainer{TContainerBuilder}"/>. Arc settles the service provider
/// options it needs while the builder is constructed, so adding Arc afterwards must leave that choice standing
/// rather than replacing it and silently dropping the application's registrations.
/// </summary>
[Collection("UsesCurrentDirectory")]
public class and_a_container_factory_is_already_configured : Specification
{
    ArcApplication? _app;
    RecordingServiceProviderFactory _factory;

    void Establish()
    {
        if (!Directory.Exists(Environment.CurrentDirectory))
        {
            Environment.CurrentDirectory = AppContext.BaseDirectory;
        }

        _factory = new RecordingServiceProviderFactory();
    }

    void Because()
    {
        var builder = new ArcApplicationBuilder(["--environment=Development"]);
        builder.ConfigureContainer(_factory);
        builder.AddCratisArc(options => options.IdentityDetailsProvider = typeof(DefaultIdentityDetailsProvider));

        _app = builder.Build();
    }

    void Destroy() => _app?.DisposeAsync().GetAwaiter().GetResult();

    [Fact] void should_build_the_service_provider_through_the_configured_factory() => _factory.Used.ShouldBeTrue();

    class RecordingServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
    {
        public bool Used { get; private set; }

        public IServiceCollection CreateBuilder(IServiceCollection services) => services;

        public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
        {
            Used = true;
            return containerBuilder.BuildServiceProvider();
        }
    }
}
