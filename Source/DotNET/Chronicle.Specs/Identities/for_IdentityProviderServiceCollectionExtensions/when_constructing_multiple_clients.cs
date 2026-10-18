// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Chronicle;
using Cratis.Chronicle.Identities;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Identities.for_IdentityProviderServiceCollectionExtensions;

public class when_constructing_multiple_clients : Specification
{
    IChronicleBuilder _builder;
    ServiceProvider _services;
    Func<IServiceProvider, object> _factory;
    int _clientConstructions;

    void Establish()
    {
        _builder = Substitute.For<IChronicleBuilder>();
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<ICurrentPrincipalAccessor>());
        services.AddSingleton<IChronicleClient>(_ =>
        {
            _clientConstructions++;
            return Substitute.For<IChronicleClient>();
        });
        services.AddArcIdentityProvider(_builder, _ => new BaseIdentityProvider(), _ => false);
        _factory = services.Last(_ => _.ServiceType == typeof(IChronicleClient)).ImplementationFactory!;
        _services = services.BuildServiceProvider();
    }

    void Because()
    {
        _factory(_services);
        _factory(_services);
    }

    [Fact] void should_install_the_identity_provider_only_once() => _builder.Received(1).IdentityProvider = _services.GetRequiredService<ArcIdentityProvider>();
    [Fact] void should_preserve_the_original_client_factory() => _clientConstructions.ShouldEqual(2);

    void Destroy() => _services.Dispose();
}
