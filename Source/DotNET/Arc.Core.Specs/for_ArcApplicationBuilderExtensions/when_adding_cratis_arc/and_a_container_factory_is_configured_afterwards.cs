// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_ArcApplicationBuilderExtensions.when_adding_cratis_arc;

/// <summary>
/// The escape hatch the documentation points applications at, held in place: whatever Arc settles for the
/// container, a later <see cref="ArcApplicationBuilder.ConfigureContainer{TContainerBuilder}"/> call replaces it.
/// This passes on both sides of the fix that made the call work in the other order too — it is here so that Arc
/// moving its own configuration later, to Build time, cannot quietly take the escape hatch away.
/// </summary>
[Collection("UsesCurrentDirectory")]
public class and_a_container_factory_is_configured_afterwards : Specification
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
        builder.AddCratisArc(options => options.IdentityDetailsProvider = typeof(DefaultIdentityDetailsProvider));
        builder.ConfigureContainer(_factory);

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
