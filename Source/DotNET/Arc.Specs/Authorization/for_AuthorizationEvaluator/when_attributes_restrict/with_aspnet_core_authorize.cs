// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator.when_attributes_restrict;

public class with_aspnet_core_authorize : given.both_attribute_families
{
    [Fact] void should_reject_a_caller_with_no_identity() =>
        (ArcFirst().IsAuthorized(typeof(ProtectedWithAspNet)) || AspNetFirst().IsAuthorized(typeof(ProtectedWithAspNet))).ShouldBeFalse();

    [Fact] void should_admit_a_signed_in_caller()
    {
        SignedInWithRoles();
        (ArcFirst().IsAuthorized(typeof(ProtectedWithAspNet)) && AspNetFirst().IsAuthorized(typeof(ProtectedWithAspNet))).ShouldBeTrue();
    }

    [Fact] void should_enforce_its_roles()
    {
        SignedInWithRoles("User");
        (ArcFirst().IsAuthorized(typeof(AdminOnlyWithAspNet)) || AspNetFirst().IsAuthorized(typeof(AdminOnlyWithAspNet))).ShouldBeFalse();
    }
}
