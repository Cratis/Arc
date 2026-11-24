// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_evaluating_declarations : given.an_evaluation
{
    [Fact] async Task should_allow_no_requirements() => (await CommandAuthorization.For<given.UnrestrictedCommand>(Provider, Principal, TestContext.Current.CancellationToken)).ShouldBeAuthorized();
    [Fact] async Task should_allow_an_authenticated_caller() => (await CommandAuthorization.For<given.AuthenticatedCommand>(Provider, Principal, TestContext.Current.CancellationToken)).ShouldBeAuthorized();
    [Fact] async Task should_deny_an_anonymous_caller() => (await CommandAuthorization.For<given.AuthenticatedCommand>(Provider, new ClaimsPrincipal(), TestContext.Current.CancellationToken)).ShouldBeDenied();
    [Fact] async Task should_honor_allow_anonymous() => (await CommandAuthorization.For<given.AnonymousCommand>(Provider, new ClaimsPrincipal(), TestContext.Current.CancellationToken)).ShouldBeAuthorized();
}
