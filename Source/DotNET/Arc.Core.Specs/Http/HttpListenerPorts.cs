// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;

namespace Cratis.Arc.Http;

internal static class HttpListenerPorts
{
    const int MaxAttempts = 10;

    internal static (HttpListener Listener, int Port) StartOnFreePort(Func<int, HttpListener> createListener)
    {
        for (var attempt = 1; ; attempt++)
        {
            using var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            var listener = createListener(port);
            try
            {
                listener.Start();
                return (listener, port);
            }
            catch (HttpListenerException) when (attempt < MaxAttempts)
            {
                listener.Close();
            }
            catch
            {
                listener.Close();
                throw;
            }
        }
    }
}
