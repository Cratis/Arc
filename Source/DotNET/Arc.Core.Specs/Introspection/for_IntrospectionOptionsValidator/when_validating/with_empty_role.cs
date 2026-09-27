// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection.for_IntrospectionOptionsValidator.when_validating;

public class with_empty_role : Specification
{
    Microsoft.Extensions.Options.ValidateOptionsResult _result;

    void Because() => _result = new IntrospectionOptionsValidator().Validate(null, new ArcOptions { Introspection = new() { RequireAuthentication = true, Roles = "Operator,  " } });

    [Fact] void should_reject_empty_role() => _result.Failed.ShouldBeTrue();
}
