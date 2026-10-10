// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_HostBuilderExtensions.when_registering_identity_source;

public class with_an_explicit_source : Specification
{
    IIdentitySource _source;
    ServiceProvider _services;

    void Establish() => _source = Substitute.For<IIdentitySource>();

    void Because() => _services = new ServiceCollection()
        .AddSingleton(_source)
        .AddCratisArcCore()
        .BuildServiceProvider();

    [Fact] void should_preserve_the_source() => _services.GetRequiredService<IIdentitySource>().ShouldBeSame(_source);

    void Destroy() => _services.Dispose();
}
