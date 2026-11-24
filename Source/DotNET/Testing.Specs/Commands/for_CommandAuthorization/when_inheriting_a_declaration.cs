// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_inheriting_a_declaration : given.an_evaluation
{
    async Task Because() => Result = await CommandAuthorization.For<given.InheritedRoleCommand>(Provider, Principal);
    [Fact] void should_apply_the_inherited_role_requirement() => Result.ShouldBeAuthorized();
}
