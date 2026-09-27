// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection.for_IntrospectionOptionsValidator.when_validating;

public class with_whitespace_around_roles : Specification
{
    ArcOptions _options;
    Microsoft.Extensions.Options.ValidateOptionsResult _result;

    void Because()
    {
        _options = new ArcOptions { Introspection = new() { RequireAuthentication = true, Roles = " Operator, Administrator " } };
        _result = new IntrospectionOptionsValidator().Validate(null, _options);
    }

    [Fact] void should_accept_roles() => _result.Succeeded.ShouldBeTrue();
    [Fact] void should_normalize_roles() => _options.Introspection.Roles.ShouldEqual("Operator,Administrator");
}
