// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Testing.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_evaluating_in_parallel_scopes : given.an_evaluation
{
    CommandAuthorizationResult[] _results;
    async Task Because()
    {
        using var first = Provider.CreateScope();
        using var second = Provider.CreateScope();
        _results = await Task.WhenAll(
            Task.Run(() => CommandAuthorization.For<given.RoleCommand>(first.ServiceProvider, Principal)),
            Task.Run(() => CommandAuthorization.For<given.RoleCommand>(second.ServiceProvider, new ClaimsPrincipal())));
    }

    [Fact] void should_authorize_only_the_authenticated_scope() => _results[0].ShouldBeAuthorized();
    [Fact] void should_deny_the_anonymous_scope() => _results[1].ShouldBeDenied();
    [Fact] void should_leave_no_ambient_principal() => Accessor.Current.ShouldBeNull();
}
