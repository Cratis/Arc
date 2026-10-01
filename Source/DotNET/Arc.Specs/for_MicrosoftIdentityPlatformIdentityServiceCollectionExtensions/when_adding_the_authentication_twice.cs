// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection.for_MicrosoftIdentityPlatformIdentityServiceCollectionExtensions;

public class when_adding_the_authentication_twice : Specification
{
    ServiceCollection _services;

    void Establish() => _services = new ServiceCollection();

    void Because()
    {
        _services.AddMicrosoftIdentityPlatformIdentityAuthentication();
        _services.AddMicrosoftIdentityPlatformIdentityAuthentication();
    }

    [Fact] void should_register_the_startup_warning_once() =>
        _services.Count(service => service.ServiceType == typeof(IHostedService) && service.ImplementationType == typeof(UntrustedForwardedIdentityHeadersStartupWarning)).ShouldEqual(1);
}
