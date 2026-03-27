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

    void Establish()
    {
        _port = GetAvailablePort();

        _logger = Substitute.For<ILogger<StaticFilesMiddleware>>();
        _middleware = new StaticFilesMiddleware(_logger);

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_port}/");

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
