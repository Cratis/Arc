// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Http.for_StaticFilesMiddleware.given;

public class a_static_files_middleware : Specification
{
    protected StaticFilesMiddleware _middleware;
    protected ILogger<StaticFilesMiddleware> _logger;
    protected HttpListener _listener;
    protected int _port;
    protected string _testDirectory;

    /// <summary>
    /// Gets the host the listener binds and the client calls, on the same address family the port probe checked.
    /// </summary>
    /// <remarks>
    /// Off Windows, .NET's managed HttpListener binds a <c>localhost</c> prefix to only the first address
    /// <c>localhost</c> resolves to - often <c>::1</c> - while the port was probed free on <c>127.0.0.1</c>, and the
    /// client resolves <c>localhost</c> on its own. The three could land on different address families, which showed up
    /// as an intermittent "Connection refused" (#2733). Windows' HTTP.sys binds both families for <c>localhost</c>, and
    /// may refuse an address prefix without elevation, so it keeps <c>localhost</c>.
    /// </remarks>
    protected static string LoopbackHost => OperatingSystem.IsWindows() ? "localhost" : "127.0.0.1";

    void Establish()
    {
        _port = GetAvailablePort();

        _logger = Substitute.For<ILogger<StaticFilesMiddleware>>();
        _middleware = new StaticFilesMiddleware(_logger);

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://{LoopbackHost}:{_port}/");

        _testDirectory = Path.Combine(Path.GetTempPath(), $"arc_specs_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    void Destroy()
    {
        _listener?.Close();
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }
}
