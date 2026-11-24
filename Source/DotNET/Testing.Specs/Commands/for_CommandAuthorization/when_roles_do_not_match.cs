// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_roles_do_not_match : given.an_evaluation
{
    async Task Because() => Result = await CommandAuthorization.For<given.RoleCommand>(Provider, new ClaimsPrincipal(new ClaimsIdentity([], "spec")));
    [Fact] void should_deny() => Result.ShouldBeDenied();
    [Fact] void should_restore_the_principal() => Accessor.Current.ShouldBeNull();
}
