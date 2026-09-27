// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection.for_IntrospectionOptionsValidator.when_validating;

public class with_trust_without_authentication : Specification
{
    Microsoft.Extensions.Options.ValidateOptionsResult _result;

    void Because() => _result = new IntrospectionOptionsValidator().Validate(null, new ArcOptions { Introspection = new() { TrustForwardedIdentityHeaders = true } });

    [Fact] void should_reject_trust_without_protection() => _result.Failed.ShouldBeTrue();
}
