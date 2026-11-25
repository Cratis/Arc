// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_preserving_an_outer_principal : given.an_evaluation
{
    bool _restored;
    async Task Because()
    {
        var outer = new ClaimsPrincipal();
        using (Accessor.BeginScope(outer))
        {
            Result = await CommandAuthorization.For<given.RoleCommand>(Provider, Principal);
            _restored = ReferenceEquals(Accessor.Current, outer);
        }
    }

    [Fact] void should_authorize_the_explicit_caller() => Result.ShouldBeAuthorized();
    [Fact] void should_restore_the_outer_principal() => _restored.ShouldBeTrue();
}
