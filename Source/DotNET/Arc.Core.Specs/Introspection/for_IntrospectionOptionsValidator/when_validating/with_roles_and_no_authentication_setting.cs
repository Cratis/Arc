// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection.for_IntrospectionOptionsValidator.when_validating;

public class with_roles_and_no_authentication_setting : Specification
{
    Microsoft.Extensions.Options.ValidateOptionsResult _result;

    void Because() => _result = new IntrospectionOptionsValidator().Validate(null, new ArcOptions { Introspection = new() { Roles = "Administrator" } });

    [Fact] void should_accept_configuration() => _result.Succeeded.ShouldBeTrue();
}
