// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;
using Cratis.Arc.Testing.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_a_discovered_availability_filter_is_ungated : given.a_discovered_filter_evaluation
{
    IAvailabilityGate _gate;
    void Establish()
    {
        _gate = Substitute.For<IAvailabilityGate>();
        _gate.IsGated(typeof(given.RoleCommand)).Returns(false);
        Services.AddSingleton(_gate);
    }

    async Task Because() => Result = await CommandAuthorization.For<given.RoleCommand>(Provider, Principal);
    [Fact] void should_authorize() => Result.ShouldBeAuthorized();
    [Fact] void should_check_the_target() => _gate.Received(1).IsGated(typeof(given.RoleCommand));
    [Fact] async Task should_not_decide_availability() => await _gate.DidNotReceive().Decide(Arg.Any<Type>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>());
}
