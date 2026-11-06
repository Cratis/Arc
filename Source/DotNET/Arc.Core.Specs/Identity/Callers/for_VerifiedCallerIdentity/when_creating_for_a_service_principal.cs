// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization.given;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerIdentity;

public class when_creating_for_a_service_principal : Specification
{
    VerifiedCallerIdentity _identity;

    void Because() => _identity = new VerifiedCallerIdentity(verified_callers.Person("ignored") with { Person = null, ServicePrincipal = new("service-1") });

    [Fact] void should_use_the_service_principal_as_subject() => _identity.FindFirst("sub")!.Value.ShouldEqual("service-1");
}
