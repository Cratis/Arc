// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Introspection.for_IntrospectionOptionsValidator.when_validating;

public class with_disabled_discovery_and_valid_access_settings : Specification
{
    ValidateOptionsResult _result;

    void Because() => _result = new IntrospectionOptionsValidator().Validate(null, new ArcOptions
    {
        Introspection = new() { Enabled = false, IdentityDiscovery = false, RequireAuthentication = true, Roles = "Administrator" }
    });

    [Fact] void should_accept_configuration() => _result.Succeeded.ShouldBeTrue();
}
