// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Http.for_HttpListenerRequestContext.given;

public class an_http_listener_request_context : Specification
{
    protected HttpListener _listener;
    protected HttpListenerContext _context;
    protected HttpListenerRequestContext _requestContext;
    protected IServiceProvider _serviceProvider;
    protected int _port;
    protected HttpClient _httpClient;

    void Establish()
    {
        (_listener, _port) = HttpListenerPorts.StartOnFreePort(port =>
        {
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
            return listener;
        });

        _serviceProvider = NSubstitute.Substitute.For<IServiceProvider>();

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{_port}/")
        };
    }

    protected async Task<HttpListenerContext> GetListenerContext()
    {
        var contextTask = _listener.GetContextAsync();
        _ = _httpClient.GetAsync("/test");
        return await contextTask;
    }

    void Destroy()
    {
        _httpClient.Dispose();
        _listener.Stop();
        _listener.Close();
    }
}
