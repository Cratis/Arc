// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_capturing_the_received_time : given.an_evaluation
{
    readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    void Establish()
    {
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(_now);
        Services.AddSingleton(clock);
        Behavior.Evaluate = (context, _) => Task.FromResult(context.ReceivedAt == _now);
    }

    async Task Because() => Result = await CommandAuthorization.For(Provider, Principal, new given.ResourceCommand(true));
    [Fact] void should_capture_receipt_using_the_configured_clock() => Result.ShouldBeAuthorized();
}
