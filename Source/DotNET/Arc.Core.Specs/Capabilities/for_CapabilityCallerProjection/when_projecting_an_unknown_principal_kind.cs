// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Capabilities.for_CapabilityCallerProjection;

public class when_projecting_an_unknown_principal_kind : Specification
{
    Exception? _exception;
    void Because()
    {
        var caller = new VerifiedCaller("1", (CallerPrincipalKind)999, CallerCredentialKind.PersonalAccessToken, new("credential"), null, new("person"), null, null, null, null, null, "resource", null, null, null, "test", new HashSet<string>(), new HashSet<string>(), null);
        _exception = Catch.Exception(() => CapabilityCallerProjection.From(caller, "capabilities", CapabilityProtocol.Rest));
    }
    [Fact] void should_fail_closed() => _exception.ShouldBeOfExactType<ArgumentOutOfRangeException>();
}
