// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_evaluating_roles : given.an_evaluation
{
    async Task Because() => Result = await CommandAuthorization.For<given.RoleCommand>(Provider, Principal);
    [Fact] void should_authorize() => Result.ShouldBeAuthorized();
    [Fact] void should_restore_the_principal() => Accessor.Current.ShouldBeNull();
}
