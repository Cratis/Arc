// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Sockets;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Hosting.for_KestrelPorts;

public class when_startup_fails_for_another_reason : Specification
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task should_dispose_the_failed_host_and_propagate_the_error_without_retrying(int failure)
    {
        Exception expected = failure switch
        {
            0 => new InvalidOperationException("Startup configuration is invalid"),
            1 => new IOException("An unrelated I/O failure"),
            2 => new OperationCanceledException(),
            _ => new IOException("Access denied", new SocketException((int)SocketError.AccessDenied))
        };
        var host = Substitute.For<IHost, IAsyncDisposable>();
        host.StartAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(expected));
        var attempts = 0;

        var error = await Catch.Exception(() => KestrelPorts.Start(
            1,
            _ =>
        {
            attempts++;
            return host;
        },
            global::Xunit.TestContext.Current.CancellationToken));

        error.ShouldBeSame(expected);
        attempts.ShouldEqual(1);
        await ((IAsyncDisposable)host).Received(1).DisposeAsync();
    }
}
