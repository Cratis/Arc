// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;
using Cratis.Arc.Testing.Commands;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_only_discovered_membership_is_registered : given.an_evaluation
{
    void Establish() => Services.AddSingleton<IInstancesOf<ITenantMembership>>(new KnownInstancesOf<ITenantMembership>([Substitute.For<ITenantMembership>()]));
    async Task Because() => Result = await CommandAuthorization.For<given.RoleCommand>(Provider, Principal);
    [Fact] void should_be_unsupported() => Result.ShouldBeUnsupported();
    [Fact] void should_identify_membership() => Result.ReasonCode.ShouldEqual("tenant-membership");
}
