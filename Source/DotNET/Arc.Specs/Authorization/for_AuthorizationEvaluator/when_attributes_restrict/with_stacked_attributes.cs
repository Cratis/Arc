// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator.when_attributes_restrict;

/// <summary>
/// Two authorization attributes on one declaration both apply, as they do in ASP.NET Core. Only the first one found
/// used to be evaluated, so the second requirement was silently dropped.
/// </summary>
public class with_stacked_attributes : given.both_attribute_families
{
    [Fact] void should_reject_a_caller_holding_only_one_required_role()
    {
        SignedInWithRoles("Admin");
        (ArcFirst().IsAuthorized(typeof(RequiringBothRoles)) || AspNetFirst().IsAuthorized(typeof(RequiringBothRoles))).ShouldBeFalse();
    }

    [Fact] void should_admit_a_caller_holding_both()
    {
        SignedInWithRoles("Admin", "Auditor");
        (ArcFirst().IsAuthorized(typeof(RequiringBothRoles)) && AspNetFirst().IsAuthorized(typeof(RequiringBothRoles))).ShouldBeTrue();
    }
}
