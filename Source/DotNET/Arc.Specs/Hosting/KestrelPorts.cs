// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Hosting;

internal static class KestrelPorts
{
    const int MaxAttempts = 10;

    internal static async Task<THost> Start<THost>(int portCount, Func<int[], THost> createHost, CancellationToken cancellationToken)
        where THost : IHost
    {
        for (var attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var host = createHost(FreePorts(portCount));
            try
            {
                await host.StartAsync(cancellationToken);
                return host;
            }
            catch (IOException exception) when (attempt < MaxAttempts && IsAddressInUse(exception))
            {
                await Dispose(host);
            }
            catch
            {
                await Dispose(host);
                throw;
            }
        }
    }

    static bool IsAddressInUse(IOException exception) =>
        exception.InnerException is AddressInUseException or SocketException { SocketErrorCode: SocketError.AddressAlreadyInUse };

    static int[] FreePorts(int count)
    {
        var listeners = Enumerable.Range(0, count).Select(_ => new TcpListener(IPAddress.Loopback, 0)).ToArray();
        try
        {
            foreach (var listener in listeners)
            {
                listener.Start();
            }
            return listeners.Select(listener => ((IPEndPoint)listener.LocalEndpoint).Port).ToArray();
        }
        finally
        {
            foreach (var listener in listeners)
            {
                listener.Stop();
            }
        }
    }

    static async Task Dispose(IHost host)
    {
        if (host is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
        else
        {
            host.Dispose();
        }
    }
}
