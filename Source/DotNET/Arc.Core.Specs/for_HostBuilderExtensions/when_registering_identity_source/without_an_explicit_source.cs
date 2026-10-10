// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_HostBuilderExtensions.when_registering_identity_source;

public class without_an_explicit_source : Specification
{
    ServiceProvider _services;
    IIdentitySource _source;

    void Establish() => _services = new ServiceCollection().AddCratisArcCore().BuildServiceProvider();

    void Because() => _source = _services.GetRequiredService<IIdentitySource>();

    [Fact] void should_register_the_default_source() => _source.ShouldBeOfExactType<IdentitySource>();
    [Fact] void should_share_the_source() => _services.GetRequiredService<IIdentitySource>().ShouldBeSame(_source);

    void Destroy() => _services.Dispose();
}
