// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Hosting.for_KestrelPorts;

public class when_every_bind_attempt_collides : Specification
{
    readonly List<IHost> _hosts = [];
    readonly IOException _failure = new("Bind failed", new AddressInUseException("Address already in use"));
    Exception? _error;

    async Task Because() => _error = await Catch.Exception(() => KestrelPorts.Start(
        2,
        _ =>
    {
        var host = Substitute.For<IHost>();
        host.StartAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(_failure));
        _hosts.Add(host);
        return host;
    },
        global::Xunit.TestContext.Current.CancellationToken));

    [Fact] void should_stop_after_ten_attempts() => _hosts.Count.ShouldEqual(10);
    [Fact] void should_dispose_every_failed_host() => _hosts.ForEach(host => host.Received(1).Dispose());
    [Fact] void should_preserve_the_final_bind_failure() => _error.ShouldBeSame(_failure);
}
